using System.Text.RegularExpressions;
using Rock3t.Telegram.Lib.Extensions;
using Rock3t.Telegram.Lib.LiteDB;
using Rock3t.Telegram.Lib.Resources;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace Rock3t.Telegram.Lib.Functions;

public class WikiModule : BotModuleBase
{
    private readonly CommonFileDatabase _fileDatabase;
    private readonly CommonFileDatabase _sentWikiArticles;
    private readonly Wiki _wiki;

    public WikiModule(ITelegramBot bot, string name) : base(bot, name)
    {
        _fileDatabase = new()
        {
            DatabaseFilePath = "./db",
            DatabaseFileName = "nouns.db"
        };
        _sentWikiArticles = new()
        {
            DatabaseFilePath = "./db",
            DatabaseFileName = "sentWikiArticles.db"
        };

        _wiki = new Wiki();
    }

    public override Guid Id { get; } = Guid.NewGuid();

    public override async Task<bool> OnUpdate(Update update)
    {
        Message? updateMessage = update.GetUpdateMessage();
        string? text = updateMessage?.Text;

        if (updateMessage?.Chat.Id == null || string.IsNullOrWhiteSpace(text))
            return false;

        if (!text.Contains("@scary_terry_the_bot") && updateMessage?.ReplyToMessage?.From?.IsBot != true)
            return false;

        NounEntity? nounEntity = null;

        List<string> words = text.Split(' ').Select(_ =>
        {
            Regex rgx = new Regex("[^a-zA-Z ß ä ö ü]");
            return rgx.Replace(_, "").ToLower().Trim();
        }).ToList();

        if (words.Contains("du"))
            words.Remove("du");
        if (words.Contains("mal"))
            words.Remove("mal");
        //var nouns = _fileDatabase.GetItems<NounEntity>();

        nounEntity = _fileDatabase.GetItems<NounEntity>()
            .FirstOrDefault(
                entity => words.Contains(entity.Name.ToLower().Trim()));

        WikiAnswer? wikiAnswer = null;

        if (nounEntity != null)
        {
            wikiAnswer = await _wiki.SearchAsync(nounEntity.Name);
        }
        else if (words.Contains("dumm"))
        {
            wikiAnswer = await _wiki.SearchAsync("dumm");
        }
        else if (words.Contains("disney"))
        {
            wikiAnswer = await _wiki.SearchAsync("disney");
        }
        else if (words.Contains("doof"))
        {
            wikiAnswer = await _wiki.SearchAsync("doof");
        }
        else if (words.Contains("blödsinn"))
        {
            wikiAnswer = await _wiki.SearchAsync("blödsinn");
        }
        else if (words.Contains("interessant"))
        {
            wikiAnswer = await _wiki.SearchAsync("interessant");
        }
        //var test = _fileDatabase.GetItems<NounEntity>().Where(n => words.Contains(n.Name.ToLower()));

        var sentWikiArticle = _sentWikiArticles.GetItems<StringEntity>();

        if (wikiAnswer != null)
        {
            await Bot.SendChatActionAsync(updateMessage.Chat.Id, ChatAction.Typing);
            Thread.Sleep(4000);

            int rnd = Random.Shared.Next(0, 11);

            var i = _sentWikiArticles.GetItems<StringEntity>()
                .FirstOrDefault(entity => entity.Value.Equals(wikiAnswer.Text));

            if (wikiAnswer.ImageUri != null && rnd >= 7 && _sentWikiArticles.GetItems<StringEntity>().FirstOrDefault(entity => entity.Value.Equals(wikiAnswer.ImageUri.ToString().GetHashCode().ToString())) == null)
            {
                await Bot.SendPhotoAsync(updateMessage.Chat.Id, new InputOnlineFile(wikiAnswer.ImageUri),
                    wikiAnswer.ImageCaption);
                
                await Task.FromResult(_sentWikiArticles.InsertItem(new StringEntity
                    { Name = "SentWikiArticle", Value = wikiAnswer.ImageUri.ToString().GetHashCode().ToString() }));
                
                return true;
            }
            else if (_sentWikiArticles.GetItems<StringEntity>().FirstOrDefault(entity => entity.Value.Equals(wikiAnswer.Text.GetHashCode().ToString())) == null)
            {
                await Bot.SendTextMessageAsync(updateMessage.Chat.Id, wikiAnswer.Text, ParseMode.Html);
                await Task.FromResult(_sentWikiArticles.InsertItem(new StringEntity { Name = "SentWikiArticle", Value = wikiAnswer.Text.GetHashCode().ToString() }));
                return true;
            }
            else
            {
                return false;
            }
        }

        return false;
    }
}