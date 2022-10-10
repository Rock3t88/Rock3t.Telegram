using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Rock3t.Telegram.Lib;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YamlDotNet.Serialization;
using File = System.IO.File;

namespace Rock3t.Telegram.Bots.CareBot;

public class CareBot : TelegramBotBase
{
    private readonly AboutMeDatabase _aboutMeDb;
    private readonly Dictionary<long, int> _aboutMeSteps;

    public List<long> AllowedGroupIds { get; private set; }
    public List<long> AllowedChannelIds { get; set; }
    public long AdminChannelId { get; private set; }
    public long FoyerChannelId { get; private set; }

    public List<Question> JoinQuestions { get; set; }

    private readonly Dictionary<long, NewMemberChat> _privateChats;
    private readonly IOptions<CareBotConfig> _options;
    private readonly ILogger<CareBot> _logger;

    internal CareBotConfig _config { get; }

    public override IBotConfig Config => _config;

    public bool IsInitialized { get; private set; }

    public CareBot(IOptions<CareBotConfig> options, ILogger<CareBot> logger) : base(options.Value.Token)
    {
        _aboutMeSteps = new Dictionary<long, int>();

        GameManager.Add<AkinatorGame>();

        _logger = logger;
        _options = options;
        _config = options.Value;

        _aboutMeDb = new AboutMeDatabase();
        //CommandManager.Commands.AddAction("lq", new CommandBase("lq", "List questions", async update =>
        //{
        //    if (Config.AdminUsers.Contains(update.Message.From.Username))
        //    {
        //        await this.SendTextMessageAsync(update.Message.Chat.Id,
        //            string.Join("\n", JoinQuestions.Select(q => $"*{JoinQuestions.IndexOf(q)}:* {q.Text}\n")), ParseMode.Markdown);
        //    }
        //}));
        //CommandManager.Commands.AddAction("lr", new CommandBase("lr", "Lists the rules", async update =>
        //{
        //    if (Config.AdminUsers.Contains(update.Message.From.Username))
        //    {
        //        await this.SendTextMessageAsync(update.Message.Chat.Id,
        //            string.Join("\n", Config.GroupRules.Select(r => $"{r}\n")), ParseMode.Markdown);
        //    }
        //}));
     
        //ToDo: CommandManager.AddAction("aboutme", "Infotext", OnAboutMe);

        if (_config.ClearUpdatesOnStart)
        {
            var clearUpdatesTask = this.GetUpdatesAsync();
            
            clearUpdatesTask.Wait();
            var updates = clearUpdatesTask.Result;

            int? offset = null;

            if (updates.Length > 0)
            {
            
                logger.LogWarning("Missed updates: ");
                foreach (Update update in updates)
                {
                    offset = update.Id;
                    var jsonString = JsonConvert.SerializeObject(update, Formatting.Indented);
                    _logger.LogWarning(jsonString);
                }

                this.GetUpdatesAsync(offset + 1).Wait();
            }
        }

        JoinQuestions = options.Value.Questions;

        _privateChats = new();

        AllowedGroupIds = new List<long>();
        AllowedChannelIds = new List<long>();
    }

    private async Task OnAboutMe(Update update)
    {
        var from = update.Message.From;

        if (from == null)
        {
            await Task.CompletedTask;
            return;
        }

        var props =
            typeof(AboutMeEntity)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p =>
                {
                    string pName = p.Name.ToLower();
                    return pName != "id" && pName != "username" && pName != "telegramname" && pName != "name";
                }).ToArray();

        int maxSteps = props.Length;

        var aboutMe = _aboutMeDb.GetItems<AboutMeEntity>().FirstOrDefault(entity => entity.UserId.Equals(from.Id));
        
        if (aboutMe == null)
        {
            if (!_aboutMeSteps.ContainsKey(from.Id))
            {
              
                _aboutMeSteps.Add(from.Id, -1);

                await this.SendTextMessageAsync(update.Message.Chat.Id,
                    "Wie ich sehe hast du noch kein Profil. Dann kannst du es jetzt anlegen :)\nFangen wir doch mal mit deinem Name an, wie heißt du?",
                    replyToMessageId: update.Message.MessageId);
            }
            else
            {
                aboutMe = new AboutMeEntity(from.Id, from.FirstName, from.Username);
                aboutMe.Name = update.Message.Text;
                _aboutMeDb.InsertItem(aboutMe);

                _aboutMeSteps[from.Id]++;

                await this.SendTextMessageAsync(update.Message.Chat.Id,
                    $"{props[_aboutMeSteps[from.Id]].Name.Replace('_', ' ')}?",
                    replyToMessageId: update.Message.MessageId);
            }


            //await this.SendTextMessageAsync(update.Message.Chat.Id, props[_aboutMeSteps[from.Id]].Name);
            
            return;

            aboutMe = new AboutMeEntity(from.Id, from.FirstName, from.Username);
            aboutMe.Geburtsdatum = "09.03.1988";
            aboutMe.Geschlecht = "Trans*weiblich";
            aboutMe.Wohnort = "Wuppertal";
            aboutMe.TelegramName = "Leonie";
            aboutMe.Andere_Plattformen = "KuMu: little-leonie";
            aboutMe.Privat_anschreiben_erlaubt = "Privat anschreiben okay.";
            _aboutMeDb.InsertItem(aboutMe);

        }
        else
        {
            if (_aboutMeSteps.ContainsKey(from.Id))
            {
                if (_aboutMeSteps[from.Id] == 0)
                {
                 
                }
                else if (_aboutMeSteps[from.Id] == maxSteps)
                {
                    _aboutMeSteps.Remove(from.Id);
                }
                else
                {
                    props[_aboutMeSteps[from.Id]].SetValue(aboutMe, update.Message.Text);
                    _aboutMeDb.UpdateItem(aboutMe);

                    _aboutMeSteps[from.Id]++;

                    if (_aboutMeSteps[from.Id] < maxSteps)
                    {
                        await this.SendTextMessageAsync(update.Message.Chat.Id,
                            $"{props[_aboutMeSteps[from.Id]].Name.Replace('_', ' ')}?",
                            replyToMessageId: update.Message.MessageId);
                    }
                    else
                    {
                        _aboutMeSteps.Remove(from.Id);

                        await this.SendTextMessageAsync(update.Message.Chat.Id,
                            $"Wunderbar. So sieht dein Profil nun aus:",
                            replyToMessageId: update.Message.MessageId);

                        string aboutMeHtmlTmp = await File.ReadAllTextAsync("./templates/aboutme.html");

                        foreach (var property in typeof(AboutMeEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public))
                        {
                            aboutMeHtmlTmp =
                                aboutMeHtmlTmp.Replace($"{{{property.Name.ToUpper()}}}",
                                    property.GetValue(aboutMe)?.ToString(), true, CultureInfo.CurrentCulture);
                        }

                        await this.SendTextMessageAsync(update.Message.Chat.Id, aboutMeHtmlTmp, ParseMode.Html);
                    }
                }
                return;
            }

            string aboutMeHtml = await File.ReadAllTextAsync("./templates/aboutme.html");

            foreach (var property in typeof(AboutMeEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                aboutMeHtml =
                    aboutMeHtml.Replace($"{{{property.Name.ToUpper()}}}",
                        property.GetValue(aboutMe)?.ToString(), true, CultureInfo.CurrentCulture);
            }

            await this.SendTextMessageAsync(update.Message.Chat.Id, aboutMeHtml, ParseMode.Html);
        }
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

        IsInitialized = true;
    }

    protected override async Task OnUpdate(Update update)
    {
        if (_aboutMeSteps.ContainsKey(update.Message.From.Id) && _aboutMeSteps[update.Message.From.Id] == -1)
        {
            _aboutMeSteps[update.Message.From.Id] = 0;
            return;
        }
        else if (_aboutMeSteps.ContainsKey(update.Message.From.Id) && _aboutMeSteps[update.Message.From.Id] >= 0)
        {
            await OnAboutMe(update);
            return;
        }

        if (update.Type == UpdateType.ChatJoinRequest) await OnNewChannelMemberJoined(update);

        var jsonString = JsonConvert.SerializeObject(update, Formatting.Indented);

        _logger.LogDebug(jsonString);

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