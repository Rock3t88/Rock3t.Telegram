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
    protected Message? LastPinnedMessage { get; set; }
    protected CommonFileDatabase Database { get; }

    protected CollectionModuleSettings? Settings { get; }

    protected Dictionary<string, int>? ListMessageIds { get; set; }
   
    protected List<UserItemMessage> AddMessages { get; } = new();
    protected List<UserItemMessage> DeleteMessages { get; } = new();
    protected List<UserItemMessage> EditMessages { get; } = new();
    //private protected Dictionary<long, List<int>> UserItemMessages { get; }

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
        //UserItemMessages = new Dictionary<long, List<int>>();
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
            ListMessageIds = Settings.ListMessageIds;
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

        await AddMessages.RemoveByUserId(message.From.Id);
        await DeleteMessages.RemoveByUserId(message.From.Id);
        await EditMessages.RemoveByUserId(message.From.Id);

        //if (UserItemMessages.ContainsKey(message.From.Id))
        //{
        //    foreach (int messageId in UserItemMessages[message.From.Id])
        //    {
        //        await Bot.DeleteMessageAsync(message.Chat.Id, messageId);
        //        UserItemMessages.Remove(message.From.Id);
        //    }
        //}
    }

    //protected void AddUserItemMessage(Message? message)
    //{
    //    if (message?.From?.Id is null)
    //        return;

    //    //foreach (int messageId in messageIds)
    //    //{
    //        if (UserItemMessages.ContainsKey(message.From!.Id))
    //        {
    //            if (!UserItemMessages[message.From!.Id].Contains(message.MessageId))
    //                UserItemMessages[message.From.Id].Add(message.MessageId);
    //        }
    //        else
    //        {
    //            UserItemMessages.Add(message.From.Id, new List<int>(new[] { message.MessageId }));
    //        }
    //    //}
    //}
    
    public override async Task<bool> OnUpdate(Update update)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message ?? update.ChannelPost;
        User? from = update.CallbackQuery?.From ?? updateMessage?.From;

        if (updateMessage?.ReplyToMessage != null && EditMessages.ContainsMessageId(updateMessage.ReplyToMessage.MessageId))
        {
            //if (updateMessage != null && updateMessage.From != null)
            //    AddUserItemMessage(updateMessage.From.Id, updateMessage.MessageId);

            var item = InternalCollection.FirstOrDefault(entity =>
                entity.Id.Equals(EditMessages.ItemByMessageId(updateMessage.ReplyToMessage.MessageId)?.EntityId));

            //EditMessages.RemoveByMessageId(updateMessage.ReplyToMessage.MessageId);
            //EditMessages.Add(updateMessage);
            await EditMessages.RemoveByUserId(updateMessage.ReplyToMessage.From?.Id);
            
            await Bot.DeleteMessageAsync(updateMessage.Chat.Id, updateMessage.MessageId);

            if (item is null)
                return await Task.FromResult(false);

            string oldValue = item.Value;
            item.Value = updateMessage.Text;

            await Task.FromResult(Database.UpdateItem(item));

            if (LastPinnedMessage != null)
            {
                Chat chat = await Bot.GetChatAsync(Bot.Config.MainChatId);

                if (chat.PinnedMessage != null)
                    await Bot.UnpinChatMessageAsync(chat.Id, chat.PinnedMessage.MessageId);
            }

            LastPinnedMessage = await Bot.SendTextMessageAsync(Bot.Config.FoyerChannelId,
                $"@{from.Username} hat etwas verändert:\n_{oldValue}_\n⬇️\n{updateMessage.Text}", ParseMode.Markdown);
            
            await OnShowItems(update);
            return true;
        }
        else if (updateMessage?.ReplyToMessage != null && AddMessages.ContainsMessageId(updateMessage.ReplyToMessage.MessageId))
        {
            await AddMessages.RemoveByUserId(updateMessage.ReplyToMessage.From?.Id);

            await Bot.DeleteMessageAsync(updateMessage.Chat.Id, updateMessage.MessageId);

            Guid id = await InsertItem(updateMessage.Chat.Id, updateMessage.From.Id, updateMessage.From.Username, updateMessage.Text);

            if (LastPinnedMessage != null)
            {
                Chat chat = await Bot.GetChatAsync(Bot.Config.MainChatId);

                if (chat.PinnedMessage != null)
                    await Bot.UnpinChatMessageAsync(chat.Id, chat.PinnedMessage.MessageId);
            }

            LastPinnedMessage = await Bot.SendTextMessageAsync(Bot.Config.FoyerChannelId,
                $"@{from.Username} hat eine neue Opfergabe hinzugefügt:\n{updateMessage.Text}", ParseMode.Markdown);

            //T collectionItem = new T();
            //collectionItem.ChatId = updateMessage.Chat.Id;
            //collectionItem.Value = updateMessage.Text;
            //collectionItem.UserId = updateMessage.From.Id;
            //collectionItem.UserName = updateMessage.From.Username;

            //Guid id = await Task.FromResult(Database.InsertItem(collectionItem));

            await OnShowItems(update);
            return true;
        }
        else if (updateMessage?.ReplyToMessage != null && DeleteMessages.ContainsMessageId(updateMessage.ReplyToMessage.MessageId))
        {
            await DeleteMessages.RemoveByUserId(updateMessage.ReplyToMessage.From?.Id);

            await Bot.DeleteMessageAsync(updateMessage.Chat.Id, updateMessage.MessageId);

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

    public async Task<Guid> InsertItem(long chatId, long userId, string userName, string item)
    {
        T collectionItem = new T();
        collectionItem.ChatId = chatId;
        collectionItem.Value = item;
        collectionItem.UserId = userId;
        collectionItem.UserName = userName;

        return await Task.FromResult(Database.InsertItem(collectionItem));
    }

    protected virtual async Task OnAddListItems(Update update, params string[] items)
    {
        Message? updateMessage = update.CallbackQuery?.Message ?? update.Message ?? update.ChannelPost;
        User? from = update.CallbackQuery?.From ?? updateMessage?.From;

        if (from is null)
            return;
       
        Message message = await Bot.SendTextMessageAsync(updateMessage.Chat.Id,
            $"Welche Opfergabe wird es bei dir {from.FirstName}?",
            replyMarkup: new ForceReplyMarkup
            {
                InputFieldPlaceholder = "Vielleicht etwas Gehirnmasse?", 
            });

        //AddUserItemMessage(message);
        AddMessages.Add(Bot, message);
    }

    protected virtual async Task OnShowItems(Update update)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message ?? update.ChannelPost;

        if (updateMessage is null) 
            return;

        //InlineKeyboardButton.WithCallbackData("hinzufugen", "add")
        //await Bot.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId);
        var users = InternalCollection.DistinctBy(entity => entity.UserName).ToArray();

        if (users.Length == 0)
            return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("*Opfergaben:*");

        foreach (var user in users)
        {
            sb.AppendLine();
            sb.AppendLine($"*@{user.UserName}:*");

            var items = InternalCollection.Where(i => i.UserId == user.UserId).OrderBy(i => i.Value).ToArray();

            foreach (var item in items)
            {
                sb.AppendLine($"- {item.Value}");
            }
        }
        
        if (ListMessageIds is null)
        {
            ListMessageIds = new Dictionary<string, int>();
            
            var message0 =
                await Bot.SendTextMessageAsync(Bot.Config.FoyerChannelId, sb.ToString(),
                    ParseMode.Markdown, disableNotification: true, protectContent: true);

            var message1 =
                await Bot.SendTextMessageAsync(Bot.Config.AdminChannelId, sb.ToString(),
                    ParseMode.Markdown, replyMarkup: InlineReplyMarkup, disableNotification: true, protectContent: true);

            await Bot.SendTextMessageAsync(Bot.Config.AdminChannelId,
                "*Neu:* Neue Opfergabe hinzufügen. (Bitte immer nur eine Sache angeben, z.B. \"Ein verschimmelter Kuchen\")\r\n" +
                "*Bearbeiten:* Bereits hinzugefügte Opfergaben nochmal ändern." +
                "\r\n*Entfernen:* Eine Opfergabe wieder entfernen.",
                ParseMode.Markdown, disableNotification: true, protectContent: true);

            ListMessageIds.Add("foyer", message0.MessageId);
            ListMessageIds.Add("admin", message1.MessageId);

            Settings!.ChatId = updateMessage.Chat.Id;
            Settings.ListMessageIds = ListMessageIds;

            Database.UpdateItem(Settings);

            await Bot.PinChatMessageAsync(Bot.Config.FoyerChannelId, message0.MessageId);
            //await Bot.PinChatMessageAsync(Bot.Config.AdminChannelId, message1.MessageId);
        }
        else
        {
            await Bot.EditMessageTextAsync(Bot.Config.AdminChannelId, ListMessageIds["admin"], sb.ToString(),
                ParseMode.Markdown, replyMarkup: InlineReplyMarkup);

            await Bot.EditMessageTextAsync(Bot.Config.FoyerChannelId, ListMessageIds["foyer"], sb.ToString(),
                ParseMode.Markdown);
        }

        await Task.CompletedTask;
    }

    protected virtual async Task OnRemoveItems(Update update, params string[] ids)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message ?? update.ChannelPost;
        User? from = update.CallbackQuery?.From ?? updateMessage?.From;

        if (from is null)
            return;

        //AddUserItemMessage(updateMessage.From?.Id, updateMessage.MessageId);

        var items = InternalCollection.Where(i => i.UserId.Equals(from.Id)).OrderBy(i => i.Value).ToArray();

        long[] userIds = InternalCollection.Select((e) => e.UserId).ToArray();

        List<InlineKeyboardButton> itemButtons = new();
        
        foreach (T item in items)
        {
            string itemText = item.Value;

            if (itemText.Length >= 64)
            {
                itemText = $"{item.Value.Substring(0, 60)}...";
            }
            itemButtons.Add(InlineKeyboardButton.WithCallbackData(itemText, $"/collection_delete {item.Id}"));
        }

        itemButtons.Add(InlineKeyboardButton.WithCallbackData("Abbrechen", "/collection_delete_cancel"));

        Message message = await Bot.SendTextMessageAsync(updateMessage.Chat.Id, $"Du hast es dir also nochmal anders überlegt {from.FirstName}? Na du wirst schon sehen was du davon hast WuhahAaha!!§!", ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(itemButtons));
     
        //AddUserItemMessage(message);
        DeleteMessages.Add(Bot, message);
    }
    
    public Task<bool> RemoveItem(Guid guid)
    {
        return Task.FromResult(Database.DeleteItem<T>(guid));
    }

    public async Task<bool> OnEditItem(Update update, Guid guid)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message ?? update.ChannelPost;
        User? from = update.CallbackQuery?.From ?? updateMessage?.From;

        if (from is null)
            return false;

        var item = InternalCollection.FirstOrDefault(entity => entity.Id.Equals(guid));

        if (item is null)
            return await Task.FromResult(false);

        string itemText = item.Value;

        if (itemText.Length >= 64)
        {
            itemText = $"{item.Value.Substring(0, 60)}...";
        }

        Message message = await Bot.SendTextMessageAsync(updateMessage.Chat.Id,
            $"Was möchtest du ändern {from.FirstName}?",
            replyMarkup: new ForceReplyMarkup
            {
                InputFieldPlaceholder = $"{itemText}"
            });

        EditMessages.Add(Bot, message, guid);

        //AddUserItemMessage(message);

        return await Task.FromResult(Database.UpdateItem(item));
    }

    public async Task<bool> OnUpdateItem(Guid guid, string text)
    {
        var item = InternalCollection.FirstOrDefault(entity => entity.Id.Equals(guid));

        if (item is null)
            return await Task.FromResult(false);

        item.Value = text;

        return await Task.FromResult(Database.UpdateItem(item));
    }

    protected virtual async Task OnUpdateItems(Update update, params string[] ids)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message ?? update.ChannelPost;
        User? from = update.CallbackQuery?.From ?? updateMessage?.From;

        if (from is null)
            return;
     
        var items = InternalCollection.Where(i => i.UserId.Equals(update.CallbackQuery.From.Id)).OrderBy(i => i.Value).ToArray();

        List<InlineKeyboardButton> itemButtons = new();

        foreach (T item in items)
        {
            string itemText = item.Value;

            if (itemText.Length >= 64)
            {
                itemText = $"{item.Value.Substring(0, 60)}...";
            }
            itemButtons.Add(InlineKeyboardButton.WithCallbackData(itemText, $"/collection_update {item.Id}"));
        }

        itemButtons.Add(InlineKeyboardButton.WithCallbackData("Abbrechen", "/collection_update_cancel"));

        Message message = await Bot.SendTextMessageAsync(updateMessage.Chat.Id, $"Du möchtest etwas ändern {from.FirstName}? Na ich hoffe für dich, dass du dein Opfer damit mindestens verdoppelst, MuhahaaAAhAH!!", ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(itemButtons));

        EditMessages.Add(Bot, message);
        //AddUserItemMessage(message);
    }
}