using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Rock3t.Telegram.Lib;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YamlDotNet.Serialization;
using File = System.IO.File;

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

    internal CareBotConfig Config { get; }

    public bool IsInitialized { get; private set; }

    public CareBot(IOptions<CareBotConfig> options) : base(options.Value.Token)
    {
        _options = options;
        Config = options.Value;

        JoinQuestions = options.Value.Questions;

        _privateChats = new();

        AllowedGroupIds = new List<long>();
        AllowedChannelIds = new List<long>();
    }

    public void Initialize()
    {
        _privateChats.Clear();

        AdminChannelId = _options.Value.AdminChannelId;
        FoyerChannelId = _options.Value.FoyerChannelId;

        AllowedChannelIds.Clear();
        AllowedGroupIds.Clear();

        AllowedChannelIds.Add(_options.Value.AdminChannelId);
        AllowedChannelIds.Add(_options.Value.FoyerChannelId);

        IsInitialized = true;

        JoinQuestions = _options.Value.Questions;

        IsInitialized = true;
    }

    protected override async Task OnUpdate(Update update)
    {
        if (update.Type == UpdateType.ChatJoinRequest) await OnNewChannelMemberJoined(update);

        var jsonString = JsonConvert.SerializeObject(update, Formatting.Indented);

        Console.WriteLine(jsonString);

        if (update.Message != null)
        {
            var chatId = update.Message.Chat.Id;

            if (!AllowedGroupIds.Contains(chatId) && !AllowedChannelIds.Contains(chatId) &&
                update.Message.Chat.Type != ChatType.Private)
                return;

            if (_privateChats.ContainsKey(update.Message.From.Id))
                await _privateChats[update.Message.From.Id].Execute(update);
            //    update.Message.Text?.Trim().ToLower().Equals(_privateChats[chatId].StartSecret?.ToLower()) == true)
            //{
            //ToDo await OnRulesAccepted(update);
            //}
        }

        //File.WriteAllText("update.json", jsonString);

        await base.OnUpdate(update);
    }

    private async Task OnNewChannelMemberJoined(Update update)
    {
        NewMemberChat? privateChat = null;

        if (_privateChats.ContainsKey(update.ChatJoinRequest.From.Id))
            privateChat = _privateChats[update.ChatJoinRequest.From.Id];

        if (privateChat == null)
        {
            await this.DeclineChatJoinRequest(update.ChatJoinRequest.Chat.Id, update.ChatJoinRequest.From.Id);
        }
        else
        {
            await this.ApproveChatJoinRequest(update.ChatJoinRequest.Chat.Id, update.ChatJoinRequest.From.Id);
            await this.RevokeChatInviteLinkAsync(update.ChatJoinRequest.Chat.Id, privateChat.InviteLink.InviteLink);
        }
    }

    protected override async void OnChatStarted(object? sender, Update update)
    {
        if (update.Message == null)
            return;

        var chatId = update.Message.Chat.Id;

        if (_privateChats.ContainsKey(update.Message.From.Id))
        {
            return;
        }
        else
        {
            var memberChat = new NewMemberChat(
                chatId, update.Message.From.Id,
                update.Message.From.Username, this, JoinQuestions.ToArray());

            await memberChat.Execute(update);

            _privateChats.Add(
                chatId, memberChat);
        }

        base.OnChatStarted(sender, update);
        await Task.CompletedTask;
    }

    protected override async Task OnChatAccepted(Update update)
    {
        if (update.Message == null)
            return;

        var chatId = update.Message.Chat.Id;

        _privateChats[update.Message.From.Id].Accepted = true;
        //if (!_privateChats.ContainsKey(update.Message.From.Id))
        //    return;

        await Task.CompletedTask;
    }
}