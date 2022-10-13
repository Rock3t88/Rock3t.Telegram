using LiteDB;
using Microsoft.Extensions.Logging;
using Rock3t.Telegram.Lib.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Rock3t.Telegram.Lib.Functions;

public class RandomTalkModule : BotModuleBase
{
    public List<string> Poems { get; private set; }
    public List<string> Facts { get; private set; }

    private List<StringEntity> SentPoems => 
        Bot.Database.GetItems<StringEntity>().Where(x => x.Name?.Equals("SentPoems") == true).ToList();

    private List<StringEntity> SentFacts =>
        Bot.Database.GetItems<StringEntity>().Where(x => x.Name?.Equals("SentFacts") == true).ToList();

    private RandomTalkModuleConfig _config;
    private Timer _timer;
    //public ILiteQueryable<StringEntity> SentPoems => 

    //public ILiteQueryable<StringEntity> SentFacts =>

    public RandomTalkModule(ITelegramBot bot, string name) : base(bot, name)
    {
        DateTime now = DateTime.UtcNow;

        var newTime = DateTime.Today
            .AddHours(now.Hour)
            .AddMinutes(now.Minute)
            .AddSeconds(now.Second + 10);
        //.AddHours(Random.Shared.Next(now.Hour, 22))
        //.AddMinutes(Random.Shared.Next(now.Minute, 60))
        //.AddSeconds(Random.Shared.Next(now.Second, 60));

        _timer = new Timer(OnTime, this, 
            (int)(newTime - now).TotalMilliseconds, Timeout.Infinite);
        
        bot.Logger.LogInformation("First fact at {newTime}", newTime);

        if (bot.Config is IHasModuleConfig<RandomTalkModuleConfig> config)
        {
            _config = config.Module;
            Poems = new List<string>(_config.Poems.Where(_ => !SentPoems.Any(entity => entity.Value.Equals(_))));
            Facts = new List<string>(_config.Facts.Where(_ => !SentFacts.Any(entity => entity.Value.Equals(_)))); 
        }
        else
        {
            throw new Exception($"Config for {nameof(RandomTalkModule)} not found!");
        }
    }

    private async void OnTime(object? state)
    {
        if (state is RandomTalkModule module)
        {
            await module.SendFact(module.Bot.Config.MainChatId);

            DateTime now = DateTime.UtcNow;
            DateTime newTime;

            if (now.Hour <= 14)
            {
                if (now.Hour < 11)
                {
                    newTime = DateTime.Today
                        .AddHours(Random.Shared.Next(14, 22))
                        .AddMinutes(Random.Shared.Next(30, 60))
                        .AddSeconds(Random.Shared.Next(0, 60));
                }
                else
                {
                    newTime = DateTime.Today
                        .AddHours(Random.Shared.Next(18, 22))
                        .AddMinutes(Random.Shared.Next(0, 60))
                        .AddSeconds(Random.Shared.Next(0, 60));
                }
            }
            else
            {
                newTime = DateTime.Today
                    .AddDays(1)
                    .AddHours(Random.Shared.Next(7, 15))
                    .AddMinutes(Random.Shared.Next(0, 60))
                    .AddSeconds(Random.Shared.Next(0, 60));
            }

            //#if DEBUG
            //                newTime = DateTime.Now.AddSeconds(5);
            //#endif

            module.Bot.Logger.LogInformation("New time to execute random fact: {newTime}", newTime);

            module._timer.Change((int)(newTime - now).TotalMilliseconds, Timeout.Infinite);
        }
    }
    
    public override Guid Id => Guid.NewGuid();

    public override async Task<bool> OnUpdate(Update update)
    {
        Message? updateMessage = update.GetUpdateMessage();

        if (updateMessage?.Text is null || (updateMessage.Chat.Type != ChatType.Group && updateMessage.Chat.Type != ChatType.Supergroup))
            return false;

        string text = updateMessage.Text;


#if DEBUG
        if (text.Contains("@scary_terry_dev_bot") || updateMessage.ReplyToMessage?.From?.IsBot == true)
#else
        if (text.Contains("@scary_terry_the_bot") || updateMessage.ReplyToMessage?.From?.IsBot == true)  
#endif
        {
            string lowerText = text.ToLower();

            if (lowerText.Contains("gedicht") || lowerText.Contains("poesie"))
            {
                await SendPoem(updateMessage.Chat.Id);
                return true;
            }
            //else if (lowerText.Contains("fakt") || lowerText.Contains("fakten"))
            //{
            //    await SendFact(updateMessage.Chat.Id);
            //    return true;
            //}
        }
        
        return await Task.FromResult(false);
    }

    private async Task SendPoem(long chatId)
    {
        await Bot.SendChatActionAsync(chatId, ChatAction.Typing);
     
        if (Poems.Count == 0)
        {
            Poems = new List<string>(_config.Poems);
        }

        int rnd = Random.Shared.Next(0, Poems.Count);

        string poem = Poems[rnd];
        
        Poems.RemoveAt(rnd);
        Bot.Database.InsertItem(new StringEntity { Name = "SentPoems", Value = poem });

        Thread.Sleep(5000);
        await Bot.SendTextMessageAsync(chatId, poem, ParseMode.Markdown);
        Bot.Logger.LogInformation("Sent poem: {poem}", poem);
    }

    private async Task SendFact(long chatId)
    {
        await Bot.SendChatActionAsync(chatId, ChatAction.Typing);
       
        if (Facts.Count == 0)
        {
            Facts = new List<string>(_config.Facts);
        }

        int rnd = Random.Shared.Next(0, Facts.Count);

        string fact = Facts[rnd];

        Facts.RemoveAt(rnd);
        Bot.Database.InsertItem(new StringEntity { Name = "SentFacts", Value = fact });
        
        Thread.Sleep(5000);
        await Bot.SendTextMessageAsync(chatId, fact, ParseMode.Markdown);

        Bot.Logger.LogInformation("Sent fact: {fact}", fact);
    }
}