using Rock3t.Telegram.Lib.LiteDB;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rock3t.Telegram.Lib.Functions;

public abstract class CollectionModuleBase<T> : BotModuleBase where T : ITelegramCollectionEntity, new()
{
    protected CommonFileDatabase Database { get; }

    protected CollectionModuleSettings? Settings { get; }

    protected int? ListMessageId { get; set; }
   
    private readonly List<long> _addMessages = new();
    private readonly List<long> _deleteMessages = new();
    private readonly Dictionary<long, Guid> _editMessages = new();
    private protected Dictionary<long, List<int>> UserItemMessages { get; private set; }

    protected virtual Collection<T> InternalCollection => new(Database.GetItems<T>().ToArray());

    public IReadOnlyCollection<T> Collection => InternalCollection.ToImmutableList();

    public override Guid Id => Guid.NewGuid();
  
    protected InlineKeyboardMarkup InlineReplyMarkup => new(new[]
    {
        InlineKeyboardButton.WithCallbackData("Neu", "/collection_add"),
        InlineKeyboardButton.WithCallbackData("Bearbeiten", "/collection_update_show"),
        InlineKeyboardButton.WithCallbackData("Entfernen", "/collection_delete_show"),
    });

    private CollectionModuleBase(ITelegramBot bot, string name, CommonFileDatabase database) : base(bot, name)
    {
        UserItemMessages = new Dictionary<long, List<int>>();
        Database = database;

        //Settings = Database.GetItems<CollectionModuleSettings>()
        //    .FirstOrDefault(settings => settings.ChatId.Equals(Bot.Config.MainChatId));
        Settings = Database.GetItems<CollectionModuleSettings>().FirstOrDefault();

        if (Settings is null)
        {
            Settings = new CollectionModuleSettings();
            Settings.ChatId = bot.Config.MainChatId;
            Database.InsertItem(Settings);
        }
        else
        {
            ListMessageId = Settings.ListMessageId;
        }
    }

    protected CollectionModuleBase(ITelegramBot bot, string name, string dbFilename, string dbFilePath = "./db") : 
        this(bot, name, new CommonFileDatabase { DatabaseFileName = dbFilename, DatabaseFilePath = dbFilePath })
    {
        
    }

    protected virtual void InitDefaultCommands()
    {
        CommandManager.AddAction("collection_show", "Show list items", OnShowItems);
        CommandManager.AddAction<string>("collection_add", "AddAction list item", OnAddListItems);
        CommandManager.AddAction<string>("collection_delete_show", "Remove list item", OnRemoveItems);
        CommandManager.AddAction<string>("collection_update_show", "Update list item", OnUpdateItems);
    }

    protected async Task DeleteUserItemMessages(Message? message)
    {
        if (message?.From?.Id == null)
            return;

        if (UserItemMessages.ContainsKey(message.From.Id))
        {
            foreach (int messageId in UserItemMessages[message.From.Id])
            {
                await Bot.DeleteMessageAsync(message.Chat.Id, messageId);
                UserItemMessages.Remove(message.From.Id);
            }
        }
    }

    protected void AddUserItemMessage(Message? message)
    {
        if (message?.From?.Id is null)
            return;

        //foreach (int messageId in messageIds)
        //{
            if (UserItemMessages.ContainsKey(message.From!.Id))
            {
                if (!UserItemMessages[message.From!.Id].Contains(message.MessageId))
                    UserItemMessages[message.From.Id].Add(message.MessageId);
            }
            else
            {
                UserItemMessages.Add(message.From.Id, new List<int>(new[] { message.MessageId }));
            }
        //}
    }
    
    public override async Task<bool> OnUpdate(Update update)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

        if (update.Message?.ReplyToMessage != null && _editMessages.ContainsKey(update.Message.ReplyToMessage.From.Id))
        {
            //if (updateMessage != null && updateMessage.From != null)
            //    AddUserItemMessage(updateMessage.From.Id, updateMessage.MessageId);

            var item = InternalCollection.FirstOrDefault(entity =>
                entity.Id.Equals(_editMessages[update.Message.ReplyToMessage.MessageId]));

            _editMessages.Remove(update.Message.ReplyToMessage.From.Id);

            await Bot.DeleteMessageAsync(update.Message.ReplyToMessage.Chat.Id, update.Message.ReplyToMessage.MessageId);
            await Bot.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId);

            if (item is null)
                return await Task.FromResult(false);


            item.Item = update.Message.Text;
            await Task.FromResult(Database.UpdateItem(item));
            await OnShowItems(update);
            return true;
        }
        else if (update.Message?.ReplyToMessage != null && _addMessages.Contains(update.Message.ReplyToMessage.From.Id))
        {
            _addMessages.Remove(update.Message.ReplyToMessage.From.Id);

            await Bot.DeleteMessageAsync(update.Message.ReplyToMessage.Chat.Id, update.Message.ReplyToMessage.MessageId);
            await Bot.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId);

            T collectionItem = new T();
            collectionItem.ChatId = update.Message.Chat.Id;
            collectionItem.Item = update.Message.Text;
            collectionItem.UserId = update.Message.From.Id;
            collectionItem.UserName = update.Message.From.Username;

            Guid id = await Task.FromResult(Database.InsertItem(collectionItem));

            await OnShowItems(update);
            return true;
        }
        else if (update.Message?.ReplyToMessage != null && _deleteMessages.Contains(update.Message.ReplyToMessage.From.Id))
        {
            _deleteMessages.Remove(update.Message.ReplyToMessage.From.Id);

            await Bot.DeleteMessageAsync(update.Message.ReplyToMessage.Chat.Id, update.Message.ReplyToMessage.MessageId);
            await Bot.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId);

            return true;
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            if (update.CallbackQuery?.Data?.StartsWith("/collection_delete_show") == true)
            {
                await OnRemoveItems(update);
                return true;
            }
            if (update.CallbackQuery?.Data?.StartsWith("/collection_update_show") == true)
            {
                await OnUpdateItems(update);
                return true;
            }
            if (update.CallbackQuery?.Data?.StartsWith("/collection_add") == true)
            {
                await OnAddListItems(update);
                return true;
            }
        }

        return false;
    }

    protected virtual async Task OnAddListItems(Update update, params string[] items)
    {
        Message? updateMessage = update.CallbackQuery?.Message ?? update.Message;

        if (updateMessage is null)
            return;
       
        Message message = await Bot.SendTextMessageAsync(updateMessage.Chat.Id,
            $"Welche Opfergabe wird es bei dir @{updateMessage.ReplyToMessage?.From?.Username}?",
            replyMarkup: new ForceReplyMarkup
            {
                InputFieldPlaceholder = "Vielleicht etwas Gehirnmasse?"
            });

        //AddUserItemMessage(message);
        _addMessages.Add(message.From.Id);
    }

    protected virtual async Task OnShowItems(Update update)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

        if (updateMessage is null) 
            return;

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
        
        if (ListMessageId is null)
        {
            var message =
                await Bot.SendTextMessageAsync(updateMessage.Chat.Id, sb.ToString(),
                    ParseMode.Markdown, replyMarkup: InlineReplyMarkup);

            ListMessageId = message.MessageId;

            Settings!.ChatId = updateMessage.Chat.Id;
            Settings.ListMessageId = (int)ListMessageId;

            Database.UpdateItem(Settings);

            await Bot.PinChatMessageAsync(updateMessage.Chat.Id, (int)ListMessageId);
        }
        else
        {
            await Bot.EditMessageTextAsync(updateMessage.Chat.Id, (int)ListMessageId, sb.ToString(), 
                ParseMode.Markdown, replyMarkup: InlineReplyMarkup);
        }

        await Task.CompletedTask;
    }

    protected virtual async Task OnRemoveItems(Update update, params string[] ids)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

        if (updateMessage is null)
            return;

        //AddUserItemMessage(updateMessage.From?.Id, updateMessage.MessageId);

        var items = InternalCollection.Where(i => i.UserId.Equals(update.CallbackQuery.From.Id)).OrderBy(i => i.Item).ToArray();

        long[] userIds = InternalCollection.Select((e) => e.UserId).ToArray();

        List<InlineKeyboardButton> itemButtons = new();

        foreach (T item in items)
        {
            string itemText = item.Item;

            if (itemText.Length >= 64)
            {
                itemText = $"{item.Item.Substring(0, 60)}...";
            }
            itemButtons.Add(InlineKeyboardButton.WithCallbackData(itemText, $"/collection_delete {item.Id}"));
        }

        Message message = await Bot.SendTextMessageAsync(updateMessage.Chat.Id, "Was möchtest du entfernen?", ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(itemButtons));
     
        //AddUserItemMessage(message);
        _deleteMessages.Add(message.MessageId);
    }
    
    public Task<bool> RemoveItem(Guid guid)
    {
        return Task.FromResult(Database.DeleteItem<T>(guid));
    }

    public async Task<bool> OnEditItem(Update update, Guid guid)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

        if (updateMessage is null)
            return false;
        
        var item = InternalCollection.FirstOrDefault(entity => entity.Id.Equals(guid));

        if (item is null)
            return await Task.FromResult(false);

        string itemText = item.Item;

        if (itemText.Length >= 64)
        {
            itemText = $"{item.Item.Substring(0, 60)}...";
        }

        Message message = await Bot.SendTextMessageAsync(updateMessage.Chat.Id,
            $"Was möchtest du ändern?",
            replyMarkup: new ForceReplyMarkup
            {
                InputFieldPlaceholder = $"{itemText}"
            });

        _editMessages.Add(message.MessageId, guid);

        AddUserItemMessage(message);

        return await Task.FromResult(Database.UpdateItem(item));
    }

    public async Task<bool> OnUpdateItem(Guid guid, string text)
    {
        var item = InternalCollection.FirstOrDefault(entity => entity.Id.Equals(guid));

        if (item is null)
            return await Task.FromResult(false);

        item.Item = text;

        return await Task.FromResult(Database.UpdateItem(item));
    }

    protected virtual async Task OnUpdateItems(Update update, params string[] ids)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

        if (updateMessage is null)
            return;

        var items = InternalCollection.Where(i => i.UserId.Equals(update.CallbackQuery.From.Id)).OrderBy(i => i.Item).ToArray();

        List<InlineKeyboardButton> itemButtons = new();

        foreach (T item in items)
        {
            string itemText = item.Item;

            if (itemText.Length >= 64)
            {
                itemText = $"{item.Item.Substring(0, 60)}...";
            }
            itemButtons.Add(InlineKeyboardButton.WithCallbackData(itemText, $"/collection_update {item.Id}"));
        }

        Message message = await Bot.SendTextMessageAsync(updateMessage.Chat.Id, "Was möchtest du ändern?", ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(itemButtons));

        AddUserItemMessage(message);
    }
}