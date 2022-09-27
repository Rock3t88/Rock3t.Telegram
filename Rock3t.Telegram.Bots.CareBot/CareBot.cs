using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Rock3t.Telegram.Lib;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Rock3t.Telegram.Bots.CareBot;

public class CareBot : TelegramBot
{
    public List<long> AllowedGroupIds { get; private set; }
    public List<long> AllowedChannelIds { get; set; }
    public long AdminChannelId { get; private set; }
    public long FoyerChannelId { get; private set; }

    public List<Question> JoinQuestions { get; set; }

    private readonly Dictionary<long, NewMemberChat> _privateChats;
    private readonly IOptions<CareBotConfig> _options;
    public ILogger<CareBot> Logger { get; }

    internal CareBotConfig Config { get; }

    public bool IsInitialized { get; private set; }

    public CareBot(IOptions<CareBotConfig> options, ILogger<CareBot> logger) : base(options.Value.Token, logger)
    {
        GameManager.Games.Add(typeof(AkinatorGame));

        Logger = logger;
        _options = options;
        Config = options.Value;

        //CommandManager.Commands.Add("lq", new Command("lq", "List questions", async update =>
        //{
        //    if (Config.AdminUsers.Contains(update.Message.From.Username))
        //    {
        //        await this.SendTextMessageAsync(update.Message.Chat.Id,
        //            string.Join("\n", JoinQuestions.Select(q => $"*{JoinQuestions.IndexOf(q)}:* {q.Text}\n")), ParseMode.Markdown);
        //    }
        //}));
        //CommandManager.Commands.Add("lr", new Command("lr", "Lists the rules", async update =>
        //{
        //    if (Config.AdminUsers.Contains(update.Message.From.Username))
        //    {
        //        await this.SendTextMessageAsync(update.Message.Chat.Id,
        //            string.Join("\n", Config.GroupRules.Select(r => $"{r}\n")), ParseMode.Markdown);
        //    }
        //}));
        
        JoinQuestions = options.Value.Questions;

        _privateChats = new();

        AllowedGroupIds = new List<long>();
        AllowedChannelIds = new List<long>();

            logger.LogConfiguration(Config);
    }

    public void Initialize()
    {
        _privateChats.Clear();

        AdminChannelId = _options.Value.AdminChannelId;
        FoyerChannelId = _options.Value.FoyerChannelId;

        AllowedChannelIds.Clear();
        AllowedGroupIds.Clear();

        AllowedChannelIds.Add(_options.Value.AdminChannelId);
        AllowedGroupIds.Add(_options.Value.FoyerChannelId);

        IsInitialized = true;

        JoinQuestions = _options.Value.Questions;

        if (Config.ClearUpdatesOnStart)
        {
            var clearUpdatesTask = this.GetUpdatesAsync();

            clearUpdatesTask.Wait();
            var updates = clearUpdatesTask.Result;

            int? offset = null;

            if (updates.Length > 0)
            {

                Logger.LogWarning("Missed updates: ");
                foreach (Update update in updates)
                {
                    offset = update.Id;
                    var jsonString = JsonConvert.SerializeObject(update, Formatting.Indented);
                    Logger.LogWarning(jsonString);
                }

                this.GetUpdatesAsync(offset + 1).Wait();
            }
        }

        IsInitialized = true;
    }

    protected override async Task OnUpdate(Update update)
    {
        if (update.Type == UpdateType.ChatJoinRequest) await OnNewChannelMemberJoined(update);

        var jsonString = JsonConvert.SerializeObject(update, Formatting.Indented);

        Logger.LogDebug(jsonString);

        if (update.Message != null)
        {
            var chatId = update.Message.Chat.Id;

            if (!AllowedGroupIds.Contains(chatId) && !AllowedChannelIds.Contains(chatId) &&
                update.Message.Chat.Type != ChatType.Private)
                return;

            if (_privateChats.ContainsKey(update.Message.From.Id))
                await _privateChats[update.Message.From.Id].Execute(update);
        }

        await base.OnUpdate(update);
    }

    private async Task OnNewChannelMemberJoined(Update update)
    {
        NewMemberChat? privateChat = null;

        if (_privateChats.ContainsKey(update.ChatJoinRequest.From.Id))
            privateChat = _privateChats[update.ChatJoinRequest.From.Id];

        if (privateChat == null)
        {
            Logger.LogDebug("{methodName}: {chatId} - {user}", 
                nameof(DeclineChatJoinRequest), update.ChatJoinRequest.Chat.Id, update.ChatJoinRequest.From.Username);

            await this.DeclineChatJoinRequest(update.ChatJoinRequest.Chat.Id, update.ChatJoinRequest.From.Id);
        }
        else
        {
            Logger.LogDebug("{methodName}: {chatId} - {user}",
                nameof(ApproveChatJoinRequest), update.ChatJoinRequest.Chat.Id, update.ChatJoinRequest.From.Username);

            await this.ApproveChatJoinRequest(update.ChatJoinRequest.Chat.Id, update.ChatJoinRequest.From.Id);
            await this.RevokeChatInviteLinkAsync(update.ChatJoinRequest.Chat.Id, privateChat.InviteLink.InviteLink);
        }
    }

    protected override async void OnChatStarted(object? sender, Update update)
    {
        if (update.Message == null)
            return;

        Logger.LogDebug("{methodName}: {chatId} - {user}",
            nameof(OnChatStarted), update.Message.Chat.Id, update.Message.From.Username);

        var chatId = update.Message.Chat.Id;

        if (_privateChats.ContainsKey(update.Message.From.Id))
            _privateChats.Remove(update.Message.From.Id);

        var memberChat = new NewMemberChat(
            chatId, update.Message.From, this, JoinQuestions.ToArray());

        await memberChat.Execute(update);

        _privateChats.Add(chatId, memberChat);

        base.OnChatStarted(sender, update);
        await Task.CompletedTask;
    }

    protected override async Task OnChatAccepted(Update update)
    {
        if (update.Message == null)
            return;

        Logger.LogDebug("{methodName}: {chatId} - {user}",
            nameof(OnChatAccepted), update.Message.Chat.Id, update.Message.From.Username);

        var chatId = update.Message.Chat.Id;

        _privateChats[update.Message.From.Id].Accepted = true;
        //if (!_privateChats.ContainsKey(update.Message.From.Id))
        //    return;

        await Task.CompletedTask;
    }
}