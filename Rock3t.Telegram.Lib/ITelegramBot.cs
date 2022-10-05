using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rock3t.Telegram.Lib;

public interface ITelegramBot : ITelegramBotClient
{
    IBotConfig Config { get; }
    Message? LastMessage { get; }
    bool IsRunning { get; }
    long GetChatId(Update update);

    Task<Message?> SendMessage(long chatId, string message, IEnumerable<KeyboardButton> buttons);
    Task SendMessage(long chatId, string message);
    Task SendImage(long chatId, Uri uri);
    Task RunAsync();
}