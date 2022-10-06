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

    protected override async Task OnShowItems(Update update)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

        //await DeleteUserItemMessages(updateMessage?.Chat.Id ?? -1, updateMessage?.From?.Id ?? -1);
        await base.OnShowItems(update);
        await DeleteUserItemMessages(updateMessage);
    }

    public override async Task<bool> OnUpdate(Update update)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;
        
        if (update.Type == UpdateType.CallbackQuery && !string.IsNullOrWhiteSpace(update.CallbackQuery!.Data) && 
            update.CallbackQuery!.Data.StartsWith("/collection_delete "))
        {
            //AddUserItemMessage(updateMessage?.From?.Id, updateMessage!.MessageId);
           
            var cmdRegex = new Regex(@"\/(?<command>\S+)( )?(?<value>.*)?", RegexOptions.IgnoreCase);

            var match = cmdRegex.Match(update.CallbackQuery.Data);

            if (match.Success)
            {
                string cmd = match.Groups["command"].Value;
                string value = match.Groups["value"].Value;

                bool canParse = Guid.TryParse(value, out Guid guid);

                if (!canParse)
                    return false;

                await RemoveItem(guid);
            }
            await OnShowItems(update);
            return true;
        }
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery.Data.StartsWith("/collection_update "))
        {
            //AddUserItemMessage(updateMessage?.From?.Id, updateMessage!.MessageId);
            //await Bot.DeleteMessageAsync(
            //    updateMessage.Chat.Id,
            //    updateMessage.MessageId);

            var cmdRegex = new Regex(@"\/(?<command>\S+)( )?(?<value>.*)?", RegexOptions.IgnoreCase);

            var match = cmdRegex.Match(update.CallbackQuery.Data);

            if (match.Success)
            {
                string cmd = match.Groups["command"].Value;
                string value = match.Groups["value"].Value;

                bool canParse = Guid.TryParse(value, out Guid guid);

                if (!canParse)
                    return false;

                await OnEditItem(update, guid);
            }
            return true;
        }
        
        bool retValue = await base.OnUpdate(update);

        return retValue;
    }

    protected override async Task OnAddListItems(Update update, params string[] items)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

        //AddUserItemMessage(updateMessage?.From?.Id, updateMessage!.MessageId);

        await base.OnAddListItems(update, items);
    }
    
    protected override async Task OnUpdateItems(Update update, params string[] ids)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

        //AddUserItemMessage(updateMessage?.From?.Id, updateMessage!.MessageId);

        await base.OnUpdateItems(update, ids);
        
        //if (_userItemMessage.ContainsKey(updateMessage.From.Id))
        //{
        //    await OnShowUserItems(update);
        //}
    }

    protected override async Task OnRemoveItems(Update update, params string[] ids)
    {
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

        //AddUserItemMessage(updateMessage?.From?.Id, updateMessage!.MessageId);

        await base.OnRemoveItems(update, ids);
        
        //if (_userItemMessage.ContainsKey(updateMessage.From?.Id ?? -1))
        //{
        //    await OnShowUserItems(update);
        //}
    }

    protected override void InitDefaultCommands()
    {
        CommandManager.AddAction("opfergaben", "Zeige alle Opfergaben", OnShowUserItems);
        CommandManager.AddAction<string>("opfergabe", "Füge eine Opfergabe hinzu", async (update, strings) =>
        {
            await OnAddListItems(update, strings);
       
        });
        CommandManager.AddAction("collection_show", "Show list items", OnShowItems, false);
        CommandManager.AddAction<string>("collection_add", "AddAction list item", async (update, strings) =>
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
        Message? updateMessage = update.Message ?? update.CallbackQuery?.Message;

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
            sb.AppendLine($"*{i}* {items[i].Item}");
        }
        
        Message message = await Bot.SendTextMessageAsync(updateMessage.Chat.Id, sb.ToString(),
            ParseMode.Markdown, replyMarkup: InlineReplyMarkup);
       
        //await Bot.DeleteMessageAsync(updateMessage.Chat.Id, updateMessage.MessageId);

       //AddUserItemMessage(updateMessage.From.Id, message.MessageId, updateMessage.MessageId);
    }

   
}