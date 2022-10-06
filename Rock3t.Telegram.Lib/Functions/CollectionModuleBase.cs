using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;
using Rock3t.Telegram.Lib.LiteDB;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rock3t.Telegram.Lib.Functions;

public abstract class CollectionModuleBase<T> : BotModuleBase where T : ITelegramCollectionEntity, new()
{
    private readonly CommonFileDatabase _database;

    private readonly CollectionModuleSettings? _settings;

    private int? _listMessageId;

    protected virtual Collection<T> InternalCollection => new(_database.GetItems<T>().ToArray());

    public IReadOnlyCollection<T> Collection => InternalCollection.ToImmutableList();

    public override Guid Id => Guid.NewGuid();

    private CollectionModuleBase(ITelegramBot bot, string name, CommonFileDatabase database) : base(bot, name)
    {
        _database = database;

        //_settings = _database.GetItems<CollectionModuleSettings>()
        //    .FirstOrDefault(settings => settings.ChatId.Equals(Bot.Config.MainChatId));
        _settings = _database.GetItems<CollectionModuleSettings>().FirstOrDefault();

        if (_settings is null)
        {
            _settings = new CollectionModuleSettings();
            _settings.ChatId = bot.Config.MainChatId;
            _database.InsertItem(_settings);
        }
        else
        {
            _listMessageId = _settings.ListMessageId;
        }
    }

    protected CollectionModuleBase(ITelegramBot bot, string name, string dbFilename, string dbFilePath = "./db") : 
        this(bot, name, new CommonFileDatabase { DatabaseFileName = dbFilename, DatabaseFilePath = dbFilePath })
    {
     
    }

    protected virtual void InitDefaultCommands()
    {
        CommandManager.AddAction("show", "Show list items", OnShowItems);
        CommandManager.AddAction<string>("add", "AddAction list item", OnAddListItems);
        CommandManager.AddAction<string>("remove", "Remove list item", OnRemoveItems);
        //CommandManager.AddAction<string>("update", "Update list item", OnUpdateItems);
    }

    protected virtual async Task OnAddListItems(Update update, params string[] items)
    {
        //await Bot.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId);
    
        foreach (string item in items)
        {
            T collectionItem = new T();
            collectionItem.ChatId = update.Message.Chat.Id;
            collectionItem.Item = item;
            collectionItem.UserId = update.Message.From.Id;
            collectionItem.UserName = update.Message.From.Username;

            Guid id = await Task.FromResult(_database.InsertItem(collectionItem));
        }

        await OnShowItems(update);
    }

    InlineKeyboardMarkup _inlineReplyMarkup = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("hinzufugen", "add"),
        InlineKeyboardButton.WithCallbackData("andern", "update"),
        InlineKeyboardButton.WithCallbackData("entfernen", "delete"),
    });

    protected virtual async Task OnShowItems(Update update)
    {
        //InlineKeyboardButton.WithCallbackData("hinzufugen", "add")
        //await Bot.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId);
        var users = InternalCollection.DistinctBy(entity => entity.UserName).ToArray();

        if (users.Length == 0)
            return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("*Opfergaben:*");
        sb.AppendLine();

        foreach (var user in users)
        {
            sb.AppendLine($"*@{user.UserName}:*");

            var items = InternalCollection.Where(i => i.UserId == user.UserId).OrderBy(i => i.Item).ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                sb.AppendLine($"*{i}* {items[i].Item}");
            }
        }
        
        if (_listMessageId is null)
        {
            var message =
                await Bot.SendTextMessageAsync(update.Message.Chat.Id, sb.ToString(),
                    ParseMode.Markdown, replyMarkup: _inlineReplyMarkup);

            _listMessageId = message.MessageId;

            _settings!.ChatId = update.Message.Chat.Id;
            _settings.ListMessageId = (int)_listMessageId;

            _database.UpdateItem(_settings);

            await Bot.PinChatMessageAsync(update.Message.Chat.Id, (int)_listMessageId);
        }
        else
        {
            await Bot.EditMessageTextAsync(update.Message.Chat.Id, (int)_listMessageId, sb.ToString(), 
                ParseMode.Markdown, replyMarkup: _inlineReplyMarkup);
        }

        await Task.CompletedTask;
    }

    protected virtual async Task OnRemoveItems(Update update, params string[] ids)
    {
        await Bot.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId);

        var items = InternalCollection.Where(i => i.UserId.Equals(update.Message.From.Id)).OrderBy(i => i.Item).ToArray();

        foreach (string strId in ids)
        {
            bool canParse = int.TryParse(strId, out int index);

            if (!canParse)
                continue;
            
            if (index < items.Length)
            {
                var id = items[index].Id;

                var item = items[index];

                bool success = await Task.FromResult(_database.DeleteItem<T>(id));

                if (success)
                {
                    await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                        $"{item.Item} wurde von @{item.UserName} entfernt.");
                }
            }
        }
        await OnShowItems(update);
    }

    protected virtual async Task OnUpdateItems(Update update, params string[] ids)
    {
        await Bot.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId);

        var items = InternalCollection.Where(i => i.UserId.Equals(update.Message.From.Id)).OrderBy(i => i.Item).ToArray();

        foreach (string strId in ids)
        {
            bool canParse = int.TryParse(strId, out int index);

            if (!canParse)
                continue;

            if (index < items.Length)
            {
                var item = items[index];
                item.Item = update.Message.Text;

                bool success = await Task.FromResult(_database.UpdateItem(item));
            }
        }
        await OnShowItems(update);
    }
}