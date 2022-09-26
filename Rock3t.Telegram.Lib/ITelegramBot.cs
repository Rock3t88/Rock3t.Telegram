using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rock3t.Telegram.Lib;

public interface ITelegramBot
{
    Message? LastMessage { get; }
    bool IsRunning { get; }
    long? BotId { get; }
    bool LocalBotServer { get; }
    TimeSpan Timeout { get; set; }
    IExceptionParser ExceptionsParser { get; set; }
    long GetChatId(Update update);

    Task<TResponse> MakeRequestAsync<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = new());

    Task<Message?> SendMessage(long chatId, string message, IEnumerable<KeyboardButton> buttons);
    Task SendMessage(long chatId, string message);
    Task SendImage(long chatId, Uri uri);
    Task RunAsync();
    Task<bool> TestApiAsync(CancellationToken cancellationToken);
    Task DownloadFileAsync(string filePath, Stream destination, CancellationToken cancellationToken);
    event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;
    event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;
}