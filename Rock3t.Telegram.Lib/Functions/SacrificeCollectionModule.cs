using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rock3t.Telegram.Lib.Functions;

public sealed class SacrificeCollectionModule : CollectionModuleBase<CollectionModuleItem>
{
    private readonly Dictionary<long, List<int>> _userItemMessage = new();

    public SacrificeCollectionModule(ITelegramBot bot, string name, string dbFilePath = "./db", string? dbFilename = null) 
        : base(bot, name, dbFilePath, dbFilename ?? $"{name}.db")
    {
        InitDefaultCommands();
    }

    //protected override async Task OnShowItems(Update update)
    //{
    //    Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

    //    //await DeleteUserItemMessages(updateMessage?.Chat.Id ?? -1, updateMessage?.From?.Id ?? -1);
    //    await base.OnShowItems(update);
    //    await DeleteUserItemMessages(updateMessage);
    //}

    public async Task ShowItems(Update update)
    {
        await OnShowItems(update);
    }

    public override async Task<bool> OnUpdate(Update update)
    {
        bool retValue = false;

        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message ?? update.ChannelPost;
        User? from = update.CallbackQuery?.From ?? updateMessage?.From;

        if (update.Type == UpdateType.CallbackQuery && !string.IsNullOrWhiteSpace(update.CallbackQuery!.Data) &&
            update.CallbackQuery!.Data.Equals("/collection_delete_cancel"))
        {
            await DeleteMessages.RemoveByUserId(updateMessage.From.Id);
            //await Bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
            return true;
        }
        if (update.Type == UpdateType.CallbackQuery && !string.IsNullOrWhiteSpace(update.CallbackQuery!.Data) &&
            update.CallbackQuery!.Data.Equals("/collection_update_cancel"))
        {
            await EditMessages.RemoveByUserId(updateMessage.From.Id);
            //await Bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
            return true;
        }
        if (update.Type == UpdateType.CallbackQuery && !string.IsNullOrWhiteSpace(update.CallbackQuery!.Data) && 
            update.CallbackQuery!.Data.StartsWith("/collection_delete "))
        {
            //AddUserItemMessage(updateMessage?.From?.Id, updateMessage!.MessageId);
           
            var cmdRegex = new Regex(@"\/(?<command>\S+)( )?(?<value>.*)?", RegexOptions.IgnoreCase);

            var match = cmdRegex.Match(update.CallbackQuery.Data);

            string? itemToDelete = null;

            if (match.Success)
            {
                string cmd = match.Groups["command"].Value;
                string value = match.Groups["value"].Value;

                bool canParse = Guid.TryParse(value, out Guid guid);

                if (!canParse)
                    return false;

                itemToDelete = InternalCollection.FirstOrDefault(item => item.Id == guid)?.Value;

                if (updateMessage?.From?.Id != null)
                {
                    await DeleteMessages.RemoveByUserId(updateMessage.From.Id);
                }
                await RemoveItem(guid);

                if (LastPinnedMessage != null)
                {
                    Chat chat = await Bot.GetChatAsync(Bot.Config.MainChatId);

                    if (chat.PinnedMessage != null)
                        await Bot.UnpinChatMessageAsync(chat.Id, chat.PinnedMessage.MessageId);
                }

                LastPinnedMessage = await Bot.SendTextMessageAsync(Bot.Config.FoyerChannelId,
                    $"@{from.Username} hat etwas entfernt:\n{itemToDelete}", ParseMode.Markdown);
            }
            await OnShowItems(update);

            //await Bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
            retValue = true;
        }
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery.Data.StartsWith("/collection_update "))
        {
            //AddUserItemMessage(updateMessage?.From?.Id, updateMessage!.MessageId);
            //await Bot.DeleteMessageAsync(
            //    updateMessage.Chat.Id,
            //    updateMessage.MessageId);

            var cmdRegex = new Regex(@"\/(?<command>\S+)( )?(?<value>.*)?", RegexOptions.IgnoreCase);

            var match = cmdRegex.Match(update.CallbackQuery.Data);
         
            string? itemToUpdate = null;

            if (match.Success)
            {
                string cmd = match.Groups["command"].Value;
                string value = match.Groups["value"].Value;

                bool canParse = Guid.TryParse(value, out Guid guid);

                if (!canParse)
                    return false;

                itemToUpdate = InternalCollection.FirstOrDefault(item => item.Id == guid)?.Value;

                if (updateMessage?.From?.Id != null)
                {
                    await EditMessages.RemoveByUserId(updateMessage.From.Id);
                }

                await OnEditItem(update, guid);
            
            }
            //await Bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
            retValue = true;
        }

        if (!retValue)
        {
            retValue = await base.OnUpdate(update);
        }

        if (!retValue)
        {
            await DeleteUserItemMessages(updateMessage);
        }

        return retValue;
    }

    //protected override async Task OnAddListItems(Update update, params string[] items)
    //{
    //    Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

    //    //AddUserItemMessage(updateMessage?.From?.Id, updateMessage!.MessageId);

    //    await base.OnAddListItems(update, items);
    //}
    
    //protected override async Task OnUpdateItems(Update update, params string[] ids)
    //{
    //    Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

    //    //AddUserItemMessage(updateMessage?.From?.Id, updateMessage!.MessageId);

    //    await base.OnUpdateItems(update, ids);
        
    //    //if (_userItemMessage.ContainsKey(updateMessage.From.Id))
    //    //{
    //    //    await OnShowUserItems(update);
    //    //}
    //}

    //protected override async Task OnRemoveItems(Update update, params string[] ids)
    //{
    //    Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

    //    //AddUserItemMessage(updateMessage?.From?.Id, updateMessage!.MessageId);

    //    await base.OnRemoveItems(update, ids);
        
    //    //if (_userItemMessage.ContainsKey(updateMessage.From?.Id ?? -1))
    //    //{
    //    //    await OnShowUserItems(update);
    //    //}
    //}

    protected override void InitDefaultCommands()
    {
        //CommandManager.AddAction("opfergaben", "Zeige alle Opfergaben", OnShowUserItems);
        CommandManager.AddAction<string>("opfergabe", "Füge eine Opfergabe hinzu", async (update, strings) =>
        {
            if (update.Message != null)
            {
                AddMessages.Add(Bot, update.Message);
            }
            await OnAddListItems(update, strings);
       
        });
        CommandManager.AddAction("collection_show", "Show list items", OnShowItems, false);
        CommandManager.AddAction<string>("collection_add", "Add list item", async (update, strings) =>
        {
            await OnAddListItems(update, strings);
          
        }, false);
        CommandManager.AddAction<string>("collection_delete_show", "Remove list item", async (update, strings) =>
        {
            await OnRemoveItems(update, strings);
           
        }, false);
        CommandManager.AddAction<string>("collection_update_show", "Update list item", async (update, strings) =>
        {
            await OnUpdateItems(update, strings);
        }, false);
        //base.InitDefaultCommands();
        //CommandManager.AddAction("opfergaben", "Show list items", OnShowItems);
        //CommandManager.AddAction("opfergabe", "Opfergabenmenü", OnShowItems);
        //CommandManager.AddAction<string>("opfergabe", "Opfergabe hinzufügen", OnAddListItems);
        //CommandManager.AddAction<string>("opfergabe_entfernen", "Opfergabe entfernen", OnRemoveItems, false);
        //CommandManager.AddAction<string>("opfergabe_ändern", "Opfergabe ändern", OnUpdateItems);
    }

    private async Task OnShowUserItems(Update update)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message ?? update.ChannelPost;;

        if (updateMessage?.From?.Id is null)
            return;

        //AddUserItemMessage(updateMessage.Chat.Id, updateMessage.MessageId);
        //await DeleteUserItemMessages(updateMessage.Chat.Id, updateMessage.From.Id);
        
        var items =
        InternalCollection.Where(item => item.UserId.Equals(updateMessage.From.Id)).ToArray();

        if (items.Length == 0)
            return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"*Opfergaben von @{updateMessage.From.Username}:*");
        sb.AppendLine();

        for (int i = 0; i < items.Length; i++)
        {
            sb.AppendLine($"*{i}* {items[i].Value}");
        }
        
        Message message = await Bot.SendTextMessageAsync(updateMessage.Chat.Id, sb.ToString(),
            ParseMode.Markdown, replyMarkup: InlineReplyMarkup);
       
        //await Bot.DeleteMessageAsync(updateMessage.Chat.Id, updateMessage.MessageId);

       //AddUserItemMessage(updateMessage.From.Id, message.MessageId, updateMessage.MessageId);
    }

   
}