using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rock3t.Telegram.Lib;



public abstract class TelegramBot : TelegramBotClient, ITelegramBot
{
    private readonly ILogger _logger;
    private CancellationToken _cancellationToken = CancellationToken.None;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    protected event EventHandler<Update>? ChatStarted;

    protected CommandManager CommandManager { get; }
    protected GameManager GameManager { get; }

    public Message? LastMessage { get; private set; }

    public bool IsRunning { get; private set; }

    protected TelegramBot(string token, ILogger logger) : base(token)
    {
        _logger = logger;
        CommandManager = new CommandManager(this);
        CommandManager.ChatStarted += OnChatStarted;
        GameManager = new GameManager();
    }

    protected virtual async void OnChatStarted(object? sender, Update update)
    {
        ChatStarted?.Invoke(this, update);
        await Task.CompletedTask;
    }

    protected abstract Task OnChatAccepted(Update update);

    public long GetChatId(Update update)
    {
        var chatId = update.Message?.Chat.Id;

        if (chatId is null)
            throw new ArgumentNullException(nameof(update.Message.Chat.Id));

        return (long)chatId;
    }

    protected virtual async Task<string> OnGameStarted(Update update, Type gameType)
    {
        if (update.Message?.From == null)
            throw new ArgumentNullException(nameof(update.Message.From));

        _logger.LogDebug("{methodName}({gameType}): {chatId} - {user}",
            gameType.Name, nameof(OnGameStarted), update.Message.Chat.Id, update.Message.From.Username);

        var game = GameManager.Create(gameType, update.Message.From, this);
        await game.StartAsync(update);

        return await Task.FromResult($"Spiel {gameType.Name} gestartet.");
    }

    protected virtual void OnErrorOccured(Exception ex)
    {
        _logger.LogError(ex.Message);
    }

    protected virtual void OnMakeRequest(IRequest request)
    {
    }

    protected virtual async Task OnUpdate(Update update)
    {
        if (update.Message?.Chat.Type == ChatType.Private)
            if (update.Message?.Text?.ToLower() == "einverstanden")
                await OnChatAccepted(update);
    }


    public override Task<TResponse> MakeRequestAsync<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = new())
    {
        OnMakeRequest(request);
        return base.MakeRequestAsync(request, cancellationToken);
    }

    public async Task<Message?> SendMessage(long chatId, string message, IEnumerable<KeyboardButton> buttons)
    {
        await this.SendChatActionAsync(chatId, ChatAction.Typing, _cancellationToken);

        _logger.LogDebug("{methodName}: {chatId} - {message}", nameof(SendMessage), chatId, message);

        //Thread.Sleep(1000);

        LastMessage = await this.SendTextMessageAsync(chatId, message, replyMarkup: new ReplyKeyboardMarkup(buttons),
            cancellationToken: _cancellationToken);

        return LastMessage;
    }

    public async Task SendMessage(long chatId, string message)
    {
        await this.SendChatActionAsync(chatId, ChatAction.Typing, _cancellationToken);
        //Thread.Sleep(3000);
        LastMessage = await this.SendTextMessageAsync(chatId, message, cancellationToken: _cancellationToken);
    }

    public async Task SendImage(long chatId, Uri uri)
    {
        _logger.LogDebug("{methodName}: {chatId} - {message}", nameof(SendImage), chatId, uri);
       
        LastMessage =
            await this.SendPhotoAsync(chatId, new InputOnlineFile(uri), cancellationToken: _cancellationToken);
    }

    public async Task RunAsync()
    {
        _cancellationToken = _cancellationTokenSource.Token;

        foreach (var gameType in GameManager.Games)
        {
            _logger.LogInformation("{methodName}: Game - {gameType}", nameof(RunAsync), gameType.Name);
        
            var gameName = gameType.Name;
            var indexOfGame = gameName.IndexOf("Game", StringComparison.Ordinal);

            if (indexOfGame > 0)
                gameName = gameName.Substring(0, indexOfGame);

            CommandManager.Commands.Add(gameName.ToLower(),
                new Command(gameName.ToLower(), $"Spiele eine Runde {gameName}",
                    update => OnGameStarted(update, gameType)));
        }

        IsRunning = true;
        await this.ReceiveAsync(OnUpdate, PollingErrorHandler, cancellationToken: _cancellationToken);
    }

    public void Stop()
    {
        _logger.LogInformation("{methodName}", nameof(Stop));
       
        _cancellationTokenSource.Cancel();
        IsRunning = false;
    }

    private async Task OnUpdate(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        foreach (var instance in GameManager.RunningGames)
        {
            await instance.Game.DoUpdates(update);
        }

        _logger.LogDebug("{methodName}", nameof(CommandManager.DoCommands));

        await CommandManager.DoCommands(update);
        await OnUpdate(update);
        await Task.CompletedTask;
    }

    private async Task PollingErrorHandler(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
    {
        OnErrorOccured(ex);
        await Task.CompletedTask;
    }

    public override string ToString()
    {
        return GetType().Name;
    }
}