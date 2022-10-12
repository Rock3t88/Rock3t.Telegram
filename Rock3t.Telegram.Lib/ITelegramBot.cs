using Microsoft.Extensions.Logging;
using Rock3t.Telegram.Lib.LiteDB;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rock3t.Telegram.Lib;

public interface ITelegramBot : ITelegramBotClient
{
    ILogger Logger { get; }
    public CommonFileDatabase Database { get; }
    IBotConfig Config { get; }
    Message? LastMessage { get; }
    bool IsRunning { get; }
    long GetChatId(Update update);

    Task<Message?> SendMessage(long chatId, string message, IEnumerable<KeyboardButton> buttons);
    Task SendMessage(long chatId, string message);
    Task SendImage(long chatId, Uri uri);
    Task RunAsync();
}