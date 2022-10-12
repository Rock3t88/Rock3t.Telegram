using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Rock3t.Telegram.Bots.ScaryTerry.Config;
using Rock3t.Telegram.Bots.ScaryTerry.db;
using Rock3t.Telegram.Lib;
using Rock3t.Telegram.Lib.Functions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Action = Rock3t.Telegram.Bots.ScaryTerry.Config.Action;

namespace Rock3t.Telegram.Bots.ScaryTerry;

public class ScaryTerryBot : TelegramBotBase
{
    private readonly Random _random = Random.Shared;
    private readonly ILogger<ScaryTerryBot> _logger;
    private readonly List<Action> _actions;
    private readonly List<Action> _randomActions;
    private readonly List<string> _welcomeMessages;
    private readonly List<string> _randomWelcomeMessages;
    private readonly Helper _helper;
    private Dictionary<string, object?>? _temporaryTokens;
    private readonly ScaryTerryDb _db;
    private readonly IOptions<ScaryTerryConfig> _options;
    private readonly HomeAssistantWrapper _ha;

    private ScaryTerryConfig _config => _options.Value;

    public override ILogger Logger => _logger;

    public override BotConfig Config => _config;

    public Dictionary<long, TelegramUser> LoggedInUsers { get; set; } = new();

    private readonly Dictionary<long, string> _chatIdToService = new();

    public ScaryTerryBot(IOptions<ScaryTerryConfig> options, ILogger<ScaryTerryBot> logger) : base(options.Value.Token)
    {
        _ha = new HomeAssistantWrapper(this);
        _logger = logger;
        _options = options;
        _helper = new();
        _randomActions = new List<Action>(_config.RandomActions);

        _logger.LogInformation(Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ??
                               Assembly.GetCallingAssembly()?.GetName()?.Version?.ToString() ?? 
                               Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() + "____1");

        _db = new ScaryTerryDb();
        _db.DatabaseFileName = "ScaryTerry.db";
        _db.DatabaseFilePath = "./db";

        Database = _db;

        Modules.Add(new RandomTalkModule(this, "random_talk"));
        Modules.Add(new SacrificeCollectionModule(this, "opfergaben"));
        Modules.Add(new WikiModule(this, "wiki"));

        CommandManager.AddAction("start_sacrifice", "Beginnt mit der Sammlung der Opfergaben", StartSacrifice, false);

        var triggeredWelcomes = _db.GetWelcomeMessages(true);

        _welcomeMessages = _db.GetWelcomeMessages(false);
        _welcomeMessages.AddRange(_config.WelcomeMessages.Where(msg => !triggeredWelcomes.Contains(msg)));

        _randomWelcomeMessages = new List<string>(_welcomeMessages);
        var users = _db.GetUsers().ToList();

        _logger.LogInformation("User Count: {0}", users.Count);

        foreach (var user in users)

        {
            LoggedInUsers.Add(user.UserId, new TelegramUser { UserId = user.UserId, Name = user.Name });
            _logger.LogInformation("{0} ({1})", user.Name, user.UserId);
        }

        foreach (Token item in _config.Tokens)
        {
            _helper.AddToken(item.Key, item.Value);
        }

        _actions = _config.Actions;

        _helper.AddToken(nameof(_config.MainChatId).ToLower(), _config.MainChatId);
        _helper.AddToken("botname", _config.Name);

        foreach (Notifier notifier in _config.Notifiers)
        {
            if (!_chatIdToService.ContainsKey(notifier.Id))
                _chatIdToService.Add(notifier.Id, notifier.Name);
        }

        _logger.LogInformation("Registered random events: {0}", _config.RandomActions.Count);

        GameManager.Add<AkinatorGame>();
        
        LogConfiguration();
    }

    protected override async void OnChatStarted(object? sender, Update update)
    {
        var collectionModule =
            Modules.FirstOrDefault(
                module => module is SacrificeCollectionModule) as SacrificeCollectionModule;

        await collectionModule?.ShowItems(update);

        base.OnChatStarted(sender, update);
    }

    private async Task StartSacrifice(Update update)
    {
        Message? updateMessage = update.Message ?? update.ChannelPost;

        if (updateMessage == null || updateMessage.Chat.Id.Equals(Config.MainChatId))
            return;
        
        await this.DeleteMessageAsync(updateMessage!.Chat.Id, updateMessage.MessageId);

        await this.SendTextMessageAsync(Config.MainChatId,
            "*Soo ihr Würmchen, nun beginnen wir mal mit der Sammlung eurer Opfergaben!*\n\n" +
            "Startet dazu einfach einen privaten Chat mir und gebt dort /start ein.\n" +
            "Außerdem gibt es einen Kanal, der euch über Änderungen informiert: https://t.me/+ZmVgwkFdEwxlY2Ni" +
            "\n\n" +
            "Um bis dahin nicht zu verhungern, habe ich auch schon eine hinzugefügt WuhahaHAhaHAaA!!!1!1!",
            ParseMode.Markdown);

        var collectionModule = Modules.FirstOrDefault(module => module is SacrificeCollectionModule) as SacrificeCollectionModule;

        await collectionModule!.InsertItem(updateMessage.Chat.Id, 5426505698, 
            "scary_terry_the_bot", "Blutige Nudelwürmer");

        await collectionModule.ShowItems(update);
    }

    protected override Task OnChatAccepted(Update update)
    {
        return Task.CompletedTask;
    }

    protected override async Task OnUpdate(Update update)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message ?? update.ChannelPost;
        global::Telegram.Bot.Types.User? user = update.CallbackQuery?.From ?? updateMessage?.From;
            
        if (updateMessage == null || updateMessage.Text == null || user?.IsBot == true || updateMessage?.IsAutomaticForward == true)
            return;

        var jsonString = JsonConvert.SerializeObject(update, Formatting.Indented);
        _logger.LogDebug(jsonString);

        try
        {
            bool result = await CommandManager.DoCommands(update);

            if (result)
                return;

            bool moduleExecuted = false;

            foreach (IBotModule botModule in Modules)
            {
                result = await botModule.CommandManager.DoCommands(update);

                if (!moduleExecuted)
                    moduleExecuted = result;

                if (result)
                    continue;

                result = await botModule.OnUpdate(update);

                if (!moduleExecuted)
                    moduleExecuted = result;

                if (moduleExecuted) 
                    break;
            }

            _logger.LogDebug($"{{{nameof(moduleExecuted)}}}:", moduleExecuted);
            _logger.LogDebug($"{{{nameof(updateMessage.From.Username)}}}:", updateMessage?.From?.Username);
            _logger.LogDebug($"{{{nameof(updateMessage.From.IsBot)}}}:", updateMessage?.From?.IsBot);

            if (moduleExecuted || updateMessage?.From?.Username == null || updateMessage?.From?.IsBot == true)
                return;

            if (updateMessage?.Chat.Id.Equals(Config.AdminChannelId) == true)
            {
                try
                {
                    await this.DeleteMessageAsync(updateMessage?.Chat.Id, updateMessage.MessageId);
                }
                catch (Exception ex)
                {
                }
            }

            _temporaryTokens = new();
            string notifierService = _chatIdToService[_config.MainChatId];

            //string botname = _config.Name;
            string botname = "scary_terry_the_bot";

            string to = "";
            Config.Action? action = null;
            TelegramText? telegramText = null;
            TelegramUser? currentUser = null;
            TelegramCommand? telegramCommand = null;

            bool isRandomAction = false;

            string? text = updateMessage?.Text;

            if (text?.StartsWith("/") == true)
            {
                try
                {
                    telegramCommand = new TelegramCommand
                    {
                        chat_id = updateMessage.Chat.Id,
                        command = text,
                        from = user,
                        id = updateMessage.MessageId,
                        user_id = user.Id
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

                if (telegramCommand.command.ToLower().Equals(_config.AddWelcomeMessageCommand.ToLower()))
                {
                    string cmdValue = telegramCommand.command.ToLower();

                    int messageIndex = cmdValue.IndexOf(_config.AddWelcomeMessageCommand.ToLower(),
                        StringComparison.Ordinal);

                    if (messageIndex >= cmdValue.Length)
                        return;

                    string message2Insert = cmdValue.Substring(messageIndex + 1);

                    _db.AddWelcomeMessage(message2Insert);
                    return;
                }

                Regex regex = new Regex(@$"((\/.*)\@{_config.Name})");

                Match match = regex.Match(telegramCommand.command);

                _logger.LogInformation("CommandBase received: {0}", telegramCommand.command);

                string strAction = null;

                if (match.Success)
                {
                    strAction = match.Value.Substring(0, match.Value.IndexOf('@'));
                }

                _logger.LogInformation("Extracted action: {0}", strAction);

                isRandomAction = strAction?.ToLower().Equals(_config.RandomCommand.ToLower()) == true;

                if (isRandomAction && _randomActions.Count == 0 && _config.RandomActions.Count > 0)
                {
                    _randomActions.AddRange(_config.RandomActions);
                }

                if (isRandomAction && _randomActions.Count > 0)
                {
                    int rnd = _random.Next(0, _randomActions.Count);
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

                    _logger.LogInformation("Config.Function triggered: {0}", action.Name);
                }
                else
                {
                    _logger.LogWarning("Config.Function not found: {0}", strAction);
                }
            }
            else if (update.Type == UpdateType.Message)
            {
                try
                {
                    //telegramText = JsonSerializer.Deserialize<TelegramText>(data.Value.ToString());
                    telegramText = new TelegramText
                    {
                        user_id = user.Id,
                        chat_id = updateMessage.Chat.Id,
                        from = user,
                        id = updateMessage.MessageId,
                        text = updateMessage.Text
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
                        {
                            _randomWelcomeMessages.AddRange(_welcomeMessages);
                        }

                        string welcomeMessage = _config.DefaultWelcomeMessage;

                        if (_randomWelcomeMessages.Count > 0)
                        {
                            int rnd = _random.Next(0, _randomWelcomeMessages.Count);
                            welcomeMessage = _randomWelcomeMessages[rnd];

                            _logger.LogInformation("Welcome message no {0} triggered.", rnd);
                        }


                        _ha.CallService(ServicesTypes.send_message,
                            data: new()
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

                        string logMessage = "Neuer Gast: {0} ({1})";
                        _logger.LogWarning(logMessage, newUser.Name, newUser.UserId);

                        _ha.CallService(
                            ServicesTypes.notify,
                            data: new()
                            {
                                message = string.Format(logMessage, user.FirstName, $"@{user.Username}"), title = "Scary Terry"
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
                    _logger.LogInformation("Message inline commandBase action found: {0}", action.Name);
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
                    int rnd = _random.Next(0, _randomActions.Count);
                    action = _randomActions[rnd];

                    if (_randomActions.Count == 0 && _config.RandomActions.Count > 0)
                    {
                        _randomActions.AddRange(_config.RandomActions);
                    }

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
        
        //await base.OnUpdate(update);
    }

    private void LogConfiguration()
    {
        _logger.LogInformation("Configuration:");
        _logger.LogInformation("Bot.Name: {0}, MainChatId: {1}", _config.Name, _config.MainChatId);

        foreach (var notifier in _config.Notifiers)
        {
            _logger.LogInformation("Notifier {0}: {1}", notifier.Name, notifier.Id);
        }

        foreach (var token in _config.Tokens)
        {
            _logger.LogInformation("Token {0}: {1}", token.Key, token.Value);
        }

        foreach (Action action in _config.Actions)
        {
            _logger.LogInformation("Config.Function {0}: {1}{2}{3}",
                action.Name, action?.MessageService?.Message,
                action?.SceneService?.Scene, action?.AudioService?.Url);
        }

        foreach (Action action in _config.RandomActions)
        {
            _logger.LogInformation("Random Config.Function {0}: {1}{2}{3}",
                action.Name, action?.MessageService?.Message,
                action?.SceneService?.Scene, action?.AudioService?.Url);
        }

        foreach (var message in _config.WelcomeMessages)
        {
            _logger.LogInformation("Message: {0}", message);
        }
    }

    private string GetTo(string text, string botname)
    {
        string to = "";

        if (text.Contains('@'))
        {
            to = Regex.Match(text, @"(?!\@" + botname + @")\@[^\s]+").Value;
        }

        AddTemporaryToken("to", to);
        return to;
    }

    private Config.Action? GetAction(string? strAction)
    {
        if (strAction == null)
            return null;

        Config.Action? action = _actions.FirstOrDefault(cmd => cmd.Name.ToLower().Equals(strAction.ToLower()));

        return action;
    }

    private Config.Action? GetInlineCommandAction(string text)
    {
        Config.Action action = null;
        string srtAction = null;
        string cmd = text;

        if (!cmd.StartsWith("-"))
            cmd = cmd.Insert(0, "-");

        foreach (Action item in _actions)
        {
            srtAction = Regex.Match(cmd, item.Name, RegexOptions.IgnoreCase).Value.ToLower();
            srtAction = _helper.ReplaceTokens(srtAction, _temporaryTokens);

            if (!string.IsNullOrWhiteSpace(srtAction))
            {
                action = item;
            }
        }

        return action;
    }

    private void AddTemporaryToken(string key, object? value)
    {
        if (!_temporaryTokens.ContainsKey(key.ToLower()))
            _temporaryTokens.Add(key.ToLower(), value);
    }

    private async void TriggerAction(Action action, long chatId, string notifierService, bool isRandomAction, TelegramUser currentUser, string? to = null)
    {
        if (action.SceneService != null && !string.IsNullOrWhiteSpace(action.SceneService.Scene))
        {
            _ha.CallService(ServicesTypes.scene,
                data: new() { entity_id = _helper.ReplaceTokens(action.SceneService.Scene, _temporaryTokens) });
        }
        if (action.AudioService != null && !string.IsNullOrWhiteSpace(action.AudioService.Url))
        {
            _ha.CallService(ServicesTypes.send_voice,
                data: new()
                {
                    url = _helper.ReplaceTokens(action.AudioService.Url, _temporaryTokens),
                    target = chatId
                });
        }
        if (action.MessageService != null && !string.IsNullOrWhiteSpace(action.MessageService.Message))
        {
            string message = action.MessageService.Message;

            if (!string.IsNullOrWhiteSpace(to))
            {
                message = message.Insert(0, $"{to} ");
            }

            _ha.CallService(ServicesTypes.send_message,
                data: new()
                {
                    message = _helper.ReplaceTokens(message, _temporaryTokens),
                    target = chatId
                });
            //_ha.CallService("notify", notifierService, data: new()
            //{
            //    message = _helper.ReplaceTokens(message, _temporaryTokens)
            //});
        }

        if (isRandomAction)
        {
            _randomActions.Remove(action);
        }
    }

    public override string ToString()
    {
        return _config.Name;
    }
}

