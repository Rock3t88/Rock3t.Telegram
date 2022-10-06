using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rock3t.Telegram.Lib.Functions;

public sealed class SacrificeCollectionModule : CollectionModuleBase<CollectionModuleItem>
{
    public SacrificeCollectionModule(ITelegramBot bot, string name, string dbFilePath = "./db", string? dbFilename = null) 
        : base(bot, name, dbFilePath, dbFilename ?? $"{name}.db")
    {
        InitDefaultCommands();
    }

    List<long> _addMessages = new();
    List<long> _updateMessages = new();
    List<long> _deleteMessages = new();

    public override async Task<bool> OnUpdate(Update update)
    {
        if (update.Message?.ReplyToMessage != null && _addMessages.Contains(update.Message.ReplyToMessage.MessageId))
        {
            _addMessages.Remove(update.Message.ReplyToMessage.MessageId);
            await OnAddListItems(update, update.Message.Text);
            return true;
        }
        if (update.Message?.ReplyToMessage != null && _deleteMessages.Contains(update.Message.ReplyToMessage.MessageId))
        {
            await Bot.DeleteMessageAsync(update.Message.ReplyToMessage.Chat.Id,
                update.Message.ReplyToMessage.MessageId);

            _deleteMessages.Remove(update.Message.ReplyToMessage.MessageId);
            await OnRemoveItems(update, update.Message.Text);
            return true;
        }
        if (update.Message?.ReplyToMessage != null && _updateMessages.Contains(update.Message.ReplyToMessage.MessageId))
        {
            await Bot.DeleteMessageAsync(update.Message.ReplyToMessage.Chat.Id,
                update.Message.ReplyToMessage.MessageId);

            _updateMessages.Remove(update.Message.ReplyToMessage.MessageId);
            await OnUpdateItems(update, update.Message.Text);
            return true;
        }

        if (update.Type == UpdateType.CallbackQuery)
        {
            if (update.CallbackQuery.Data == "add")
            {
                Message message = await Bot.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id,
                    $"Welche Opfergabe wird es bei dir @{update.CallbackQuery.From.Username}?", ParseMode.Markdown,
                    replyMarkup: new ForceReplyMarkup
                    {
                        InputFieldPlaceholder = "Vielleicht etwas Gehirnmasse?"
                    });

                _addMessages.Add(message.MessageId);
                return true;
            }

            if (update.CallbackQuery.Data == "delete")
            {
                int count = InternalCollection.Count(item => item.UserId.Equals(update.CallbackQuery.From.Id));

                Message message = await Bot.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"Welche Opfergabe soll entfernt werden @{update.CallbackQuery.From.Username}?", ParseMode.Markdown,
                    replyMarkup: new ForceReplyMarkup
                    {
                        InputFieldPlaceholder = $"0 - {count - 1}"
                    });

                _deleteMessages.Add(message.MessageId);
                return true;
            }

            if (update.CallbackQuery.Data == "update")
            {
                int count = InternalCollection.Count(item => item.UserId.Equals(update.CallbackQuery.From.Id));

                Message message = await Bot.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"Welche Opfergabe soll geändert werden @{update.CallbackQuery.From.Username}?", ParseMode.Markdown,
                    replyMarkup: new ForceReplyMarkup
                    {
                        InputFieldPlaceholder = $"0 - {count - 1}"
                    });

                _updateMessages.Add(message.MessageId);
                return true;
            }
        }
        
        return await Task.FromResult(false);
    }

    protected override void InitDefaultCommands()
    {
        //CommandManager.AddAction("opfergaben", "Show list items", OnShowItems);
        //CommandManager.AddAction("opfergabe", "Opfergabenmenü", OnShowItems);
        CommandManager.AddAction<string>("opfergabe", "Opfergabe hinzufügen", OnAddListItems);
        CommandManager.AddAction<string>("opfergabe_entfernen", "Opfergabe entfernen", OnRemoveItems, false);
        //CommandManager.AddAction<string>("opfergabe_ändern", "Opfergabe ändern", OnUpdateItems);
    }

    private async Task OnShowOptions(Update update)
    {
        //OnShowItems()
        //await Bot.SendTextMessageAsync(update.Message.Chat.Id, "Opfergaben", ParseMode.Markdown,
        //    replyMarkup: new InlineKeyboardMarkup(new []
        //    {
        //        InlineKeyboardButton.WithCallbackData("hinzufugen", "add"),
        //        InlineKeyboardButton.WithCallbackData("andern", "update"),
        //        InlineKeyboardButton.WithCallbackData("entfernen", "delete"),
        //    })
        //    {
                
        //        //new KeyboardButton("Opfergabe hinzufügen"),
        //        //new KeyboardButton("Opfergabe ändern"),
        //        //new KeyboardButton("Opfergabe entfernen"),
        //    }); // { OneTimeKeyboard = true, InputFieldPlaceholder = "id"});
    }
}