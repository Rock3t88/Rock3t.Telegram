using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rock3t.Telegram.Bots.ScaryTerry.Config;
using Rock3t.Telegram.Bots.ScaryTerry.db;
using Rock3t.Telegram.Lib;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Action = Rock3t.Telegram.Bots.ScaryTerry.Config.Action;

namespace Rock3t.Telegram.Bots.ScaryTerry;

public class ScaryTerryBotBase : TelegramBotBase
{
    private readonly Random _random = Random.Shared;
    private readonly ILogger<ScaryTerryBotBase> _logger;
    private readonly List<Action> _actions;
    private readonly List<Action> _randomActions;
    private readonly List<string> _welcomeMessages;
    private readonly List<string> _randomWelcomeMessages;
    private readonly Helper _helper;
    private Dictionary<string, object?>? _temporaryTokens;
    private readonly ScaryTerryDb _db;
    private readonly IOptions<ScaryTerryConfig> _options;
    private readonly HomeAssistantWrapper _ha;

    public ScaryTerryConfig Config => _options.Value;

    public Dictionary<long, TelegramUser> LoggedInUsers { get; set; } = new();

    private readonly Dictionary<long, string> _chatIdToService = new();

    public ScaryTerryBotBase(IOptions<ScaryTerryConfig> options, ILogger<ScaryTerryBotBase> logger) : base(options.Value
        .Token)
    {
        _ha = new HomeAssistantWrapper(this);
        _logger = logger;
        _options = options;
        _helper = new Helper();
        _randomActions = new List<Action>(Config.RandomActions);

        _db = new ScaryTerryDb();
        _db.DatabaseFileName = "ScaryTerry.db";
        _db.DatabaseFilePath = "./db";

        var triggeredWelcomes = _db.GetWelcomeMessages(true);

        _welcomeMessages = _db.GetWelcomeMessages(false);
        _welcomeMessages.AddRange(Config.WelcomeMessages.Where(msg => !triggeredWelcomes.Contains(msg)));

        _randomWelcomeMessages = new List<string>(_welcomeMessages);
        var users = _db.GetUsers().ToList();

        _logger.LogInformation("User Count: {0}", users.Count);

        foreach (var user in users)

        {
            LoggedInUsers.Add(user.UserId, new TelegramUser { UserId = user.UserId, Name = user.Name });
            _logger.LogInformation("{0} ({1})", user.Name, user.UserId);
        }

        foreach (var item in Config.Tokens) _helper.AddToken(item.Key, item.Value);

        _actions = Config.Actions;

        _helper.AddToken(nameof(Config.MainChatId).ToLower(), Config.MainChatId);
        _helper.AddToken("botname", Config.Name);

        foreach (var notifier in Config.Notifiers)
            if (!_chatIdToService.ContainsKey(notifier.Id))
                _chatIdToService.Add(notifier.Id, notifier.Name);

        _logger.LogInformation("Registered random events: {0}", Config.RandomActions.Count);

        GameManager.Games.Add(typeof(AkinatorGame));

        LogConfiguration();
    }

    protected override Task OnChatAccepted(Update update)
    {
        return Task.CompletedTask;
    }

    protected override async Task OnUpdate(Update update)
    {
        try
        {
            _temporaryTokens = new Dictionary<string, object?>();
            var notifierService = _chatIdToService[Config.MainChatId];

            var botname = Config.Name;
            var to = "";
            Action? action = null;
            TelegramText? telegramText = null;
            TelegramUser? currentUser = null;
            TelegramCommand? telegramCommand = null;

            var isRandomAction = false;

            var text = update.Message?.Text;

            if (text?.StartsWith("/") == true)
            {
                try
                {
                    telegramCommand = new TelegramCommand
                    {
                        chat_id = update.Message.Chat.Id,
                        command = text,
                        from = update.Message.From,
                        id = update.Message.MessageId,
                        user_id = update.Message.From.Id
                    };
                    //telegramCommand = JsonSerializer.Deserialize<TelegramCommand>(data.Value.ToString());

                    if (!_chatIdToService.ContainsKey(telegramCommand?.chat_id ?? -1))
                        return;

                    notifierService = _chatIdToService[telegramCommand?.chat_id ?? 0];
                    AddTemporaryToken(nameof(notifierService), notifierService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }

                if (telegramCommand == null)
                    return;

                _logger.LogInformation("chat id {0}", telegramCommand.chat_id);

                if (!_chatIdToService.ContainsKey(telegramCommand.chat_id))
                    return;

                if (telegramCommand.command.ToLower().Equals(Config.AddWelcomeMessageCommand.ToLower()))
                {
                    var cmdValue = telegramCommand.command.ToLower();

                    var messageIndex = cmdValue.IndexOf(Config.AddWelcomeMessageCommand.ToLower(),
                        StringComparison.Ordinal);

                    if (messageIndex >= cmdValue.Length)
                        return;

                    var message2Insert = cmdValue.Substring(messageIndex + 1);

                    _db.AddWelcomeMessage(message2Insert);
                    return;
                }

                var regex = new Regex(@$"((\/.*)\@{Config.Name})");

                var match = regex.Match(telegramCommand.command);

                _logger.LogInformation("Command received: {0}", telegramCommand.command);

                string strAction = null;

                if (match.Success) strAction = match.Value.Substring(0, match.Value.IndexOf('@'));

                _logger.LogInformation("Extracted action: {0}", strAction);

                isRandomAction = strAction?.ToLower().Equals(Config.RandomCommand.ToLower()) == true;

                if (isRandomAction && _randomActions.Count == 0 && Config.RandomActions.Count > 0)
                    _randomActions.AddRange(Config.RandomActions);

                if (isRandomAction && _randomActions.Count > 0)
                {
                    var rnd = _random.Next(0, _randomActions.Count);
                    action = _randomActions[rnd];

                    _logger.LogInformation("Random action no {0} triggered: {1}", rnd, action.Name);
                    _logger.LogInformation("Random actions left: {0}", _randomActions.Count);
                }
                else
                {
                    action = GetAction(strAction);
                }

                if (action != null)
                {
                    TriggerAction(action, telegramCommand.chat_id, notifierService, isRandomAction, currentUser);

                    _logger.LogInformation("Config.Action triggered: {0}", action.Name);
                }
                else
                {
                    _logger.LogWarning("Config.Action not found: {0}", strAction);
                }
            }
            else if (update.Type == UpdateType.Message)
            {
                try
                {
                    //telegramText = JsonSerializer.Deserialize<TelegramText>(data.Value.ToString());
                    telegramText = new TelegramText
                    {
                        user_id = update.Message.From.Id,
                        chat_id = update.Message.Chat.Id,
                        from = update.Message.From,
                        id = update.Message.MessageId,
                        text = update.Message.Text
                    };

                    if (!_chatIdToService.ContainsKey(telegramText?.chat_id ?? -1))
                        return;

                    notifierService = _chatIdToService[telegramText?.chat_id ?? 0];
                    AddTemporaryToken(nameof(notifierService), notifierService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }

                if (telegramText != null)
                {
                    text = telegramText?.text;

                    _logger.LogInformation("chat id {0}", telegramText.chat_id);
                    notifierService = _chatIdToService[telegramText.chat_id];

                    if (LoggedInUsers.ContainsKey(telegramText.user_id) == true)
                    {
                        currentUser = LoggedInUsers[telegramText.user_id];
                    }
                    else
                    {
                        var newUser = new TelegramUser();
                        newUser.UserId = telegramText.user_id;
                        newUser.Name = telegramText.from.FirstName;
                        LoggedInUsers.Add(newUser.UserId, newUser);
                        currentUser = newUser;

                        AddTemporaryToken("currentuserid", currentUser.UserId);
                        AddTemporaryToken("currentusername", currentUser.Name);

                        if (_randomWelcomeMessages.Count == 0 && _welcomeMessages.Count > 0)
                            _randomWelcomeMessages.AddRange(_welcomeMessages);

                        var welcomeMessage = Config.DefaultWelcomeMessage;

                        if (_randomWelcomeMessages.Count > 0)
                        {
                            var rnd = _random.Next(0, _randomWelcomeMessages.Count);
                            welcomeMessage = _randomWelcomeMessages[rnd];

                            _logger.LogInformation("Welcome message no {0} triggered.", rnd);
                        }


                        _ha.CallService(ServicesTypes.send_message,
                            new MessageData
                            {
                                message = _helper.ReplaceTokens(string.Format(welcomeMessage, currentUser?.Name),
                                    _temporaryTokens),
                                target = telegramText.chat_id
                            });
                        //_ha.CallService("notify", notifierService, data: new()
                        //{
                        //    message =
                        //        _helper.ReplaceTokens(
                        //            string.Format(welcomeMessage, currentUser?.Name), _temporaryTokens)

                        //    //message = $"Schreckliches Willkommen {currentUser?.Name}! " +
                        //    //          $"Ich hoffe du schläfst früh genug ein, denn dann wirst du nie wieder aufwachen, das verspreche ich dir! Muhahahahhahaaaa!!!"
                        //});

                        _randomWelcomeMessages.Remove(welcomeMessage);
                        _db.AddWelcomeMessage(welcomeMessage, true);

                        var logMessage = "Neuer Gast: {0} ({1})";
                        _logger.LogWarning(logMessage, newUser.Name, newUser.UserId);

                        _ha.CallService(
                            ServicesTypes.notify,
                            new MessageData
                            {
                                message = string.Format(logMessage, newUser.Name, newUser.UserId), title = "Scary Terry"
                            });

                        _db.CreateUser(newUser.UserId, newUser.Name);
                    }
                }

                //string? text = telegramText?.text;

                if (text == null)
                    return;

                text = _helper.ReplaceTokens(text, _temporaryTokens);

                _logger.LogDebug("Telegram event received.  Current user: {0} ({1}); message: {2}",
                    currentUser?.Name, currentUser?.UserId, text);

                if (text?.Contains(botname) != true)
                    return;

                action = GetInlineCommandAction(text);

                if (action != null)
                {
                    _logger.LogInformation("Message inline command action found: {0}", action.Name);
                    TriggerAction(action, telegramText.chat_id, notifierService, false, currentUser);
                }

                to = GetTo(text, botname);

                //if (string.IsNullOrWhiteSpace(to))
                //{


                //    //_ha.CallService("notify", notifierService, data: new()
                //    //{
                //    //    message = _helper.ReplaceTokens(
                //    //        string.Format(
                //    //            Config.DefaultMessage, telegramText?.from_first), _temporaryTokens)

                //    //    //message = $"Keine Ahnung was du von mir willst {telegramText.from_first}!"
                //    //});
                //    return;
                //}

                if (action == null)
                {
                    var rnd = _random.Next(0, _randomActions.Count);
                    action = _randomActions[rnd];

                    if (_randomActions.Count == 0 && Config.RandomActions.Count > 0)
                        _randomActions.AddRange(Config.RandomActions);

                    TriggerAction(action, telegramText.chat_id, notifierService, true, currentUser, to);

                    _logger.LogInformation("Random action no {0} triggered: {1}", rnd, action.Name);

                    //_ha.CallService("notify", notifierService, data: new()
                    //{
                    //    message = _helper.ReplaceTokens(string.Format(Config.DefaultRecipientMessage, to, telegramText?.from_first), _temporaryTokens)

                    //    //message = $"Hey {to}! {telegramText.from_first} will etwas von dir! Keine Ahnung was ich dazu sagen soll."
                    //});
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        await base.OnUpdate(update);
    }

    private void LogConfiguration()
    {
        _logger.LogInformation("Configuration:");
        _logger.LogInformation("BotBase.Name: {0}, MainChatId: {1}", Config.Name, Config.MainChatId);

        foreach (var notifier in Config.Notifiers)
            _logger.LogInformation("Notifier {0}: {1}", notifier.Name, notifier.Id);

        foreach (var token in Config.Tokens) _logger.LogInformation("Token {0}: {1}", token.Key, token.Value);

        foreach (var action in Config.Actions)
            _logger.LogInformation("Config.Action {0}: {1}{2}{3}",
                action.Name, action?.MessageService?.Message,
                action?.SceneService?.Scene, action?.AudioService?.Url);

        foreach (var action in Config.RandomActions)
            _logger.LogInformation("Random Config.Action {0}: {1}{2}{3}",
                action.Name, action?.MessageService?.Message,
                action?.SceneService?.Scene, action?.AudioService?.Url);

        foreach (var message in Config.WelcomeMessages) _logger.LogInformation("Message: {0}", message);
    }

    private string GetTo(string text, string botname)
    {
        var to = "";

        if (text.Contains('@')) to = Regex.Match(text, @"(?!\@" + botname + @")\@[^\s]+").Value;

        AddTemporaryToken("to", to);
        return to;
    }

    private Action? GetAction(string? strAction)
    {
        if (strAction == null)
            return null;

        var action = _actions.FirstOrDefault(cmd => cmd.Name.ToLower().Equals(strAction.ToLower()));

        return action;
    }

    private Action? GetInlineCommandAction(string text)
    {
        Action action = null;
        string srtAction = null;
        var cmd = text;

        if (!cmd.StartsWith("-"))
            cmd = cmd.Insert(0, "-");

        foreach (var item in _actions)
        {
            srtAction = Regex.Match(cmd, item.Name, RegexOptions.IgnoreCase).Value.ToLower();
            srtAction = _helper.ReplaceTokens(srtAction, _temporaryTokens);

            if (!string.IsNullOrWhiteSpace(srtAction)) action = item;
        }

        return action;
    }

    private void AddTemporaryToken(string key, object? value)
    {
        if (!_temporaryTokens.ContainsKey(key.ToLower()))
            _temporaryTokens.Add(key.ToLower(), value);
    }

    private async void TriggerAction(Action action, long chatId, string notifierService, bool isRandomAction,
        TelegramUser currentUser, string? to = null)
    {
        if (action.SceneService != null && !string.IsNullOrWhiteSpace(action.SceneService.Scene))
            _ha.CallService(ServicesTypes.scene,
                new MessageData { entity_id = _helper.ReplaceTokens(action.SceneService.Scene, _temporaryTokens) });
        if (action.AudioService != null && !string.IsNullOrWhiteSpace(action.AudioService.Url))
            _ha.CallService(ServicesTypes.send_voice,
                new MessageData
                {
                    url = _helper.ReplaceTokens(action.AudioService.Url, _temporaryTokens),
                    target = chatId
                });
        if (action.MessageService != null && !string.IsNullOrWhiteSpace(action.MessageService.Message))
        {
            var message = action.MessageService.Message;

            if (!string.IsNullOrWhiteSpace(to)) message = message.Insert(0, $"{to} ");

            _ha.CallService(ServicesTypes.send_message,
                new MessageData
                {
                    message = _helper.ReplaceTokens(message, _temporaryTokens),
                    target = chatId
                });
            //_ha.CallService("notify", notifierService, data: new()
            //{
            //    message = _helper.ReplaceTokens(message, _temporaryTokens)
            //});
        }

        if (isRandomAction) _randomActions.Remove(action);
    }

    public override string ToString()
    {
        return Config.Name;
    }
}