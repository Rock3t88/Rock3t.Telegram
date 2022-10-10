using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using Rock3t.MetaProperties;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Rock3t.Telegram.Lib.Extensions;

public static class MetaPropertyExtensions
{
    public static Message? GetUpdateMessage(this Update update)
    {
        return update.Message ?? update.CallbackQuery?.Message ?? update.ChannelPost;
    }

    public static User? GetFrom(this Update update)
    {
        return update.CallbackQuery?.From ?? update.GetUpdateMessage()?.From;
    }

    public static async Task Answer(this Update update, string text, bool isReply = true)
    {
        Message? updateMessage = update.GetUpdateMessage();
        User? from = update.GetFrom();

        if (updateMessage == null)
            return;
        
        long chatId = updateMessage.Chat.Id;

        var bot = (TelegramBotBase)update.GetMetaProperty<TelegramBotBase>("Bot");
       
        await bot.SendChatActionAsync(chatId, ChatAction.Typing);

        string tmpText = text.ReplaceTokens(updateMessage);
        
        Thread.Sleep(1500);

        if (isReply)
        {
           await bot.SendTextMessageAsync(
                chatId, tmpText, ParseMode.Markdown, replyToMessageId: updateMessage.MessageId);
        }
        else
        {
            await bot.SendTextMessageAsync(
                chatId, tmpText, ParseMode.Markdown);
        }
    }

    public static string ReplaceTokens(this string text, Message message)
    {
        //string[] replaceTokens = new[]
        //{
        //    "userName",
        //    "firstName",
        //    "lastName",
        //};

        string tmpText = text;

        //foreach (var replaceToken in replaceTokens)
        //{
        //string token = ;
        string tph = Constants.TokenPlaceholder;

        tmpText = Regex.Replace(tmpText,
            $"{tph}From.Username{tph}", message.From?.Username ?? "");

        tmpText = Regex.Replace(tmpText,
            $"{tph}From.FirstName{tph}", message.From?.FirstName ?? "");

        tmpText = Regex.Replace(tmpText,
            $"{tph}From.FirstName{tph}", message.From?.FirstName ?? "");
        //}

        return tmpText;
    }
}