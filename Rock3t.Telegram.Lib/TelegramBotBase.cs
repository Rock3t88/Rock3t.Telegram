﻿using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rock3t.Telegram.Lib;

public interface ITelegramBotBase
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

public abstract class TelegramBotBase : TelegramBotClient, ITelegramBotBase
{
    private CancellationToken _cancellationToken = CancellationToken.None;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    protected event EventHandler<Update>? ChatStarted;

    protected CommandManager CommandManager { get; }
    protected GameManager GameManager { get; }

    public Message? LastMessage { get; private set; }

    public bool IsRunning { get; private set; }

    protected TelegramBotBase(string token) : base(token)
    {
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

        var game = GameManager.Create(gameType, update.Message.From, this);
        await game.StartAsync(update);

        return await Task.FromResult($"Spiel {gameType.Name} gestartet.");
    }

    protected virtual void OnErrorOccured(Exception ex)
    {
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
        LastMessage =
            await this.SendPhotoAsync(chatId, new InputOnlineFile(uri), cancellationToken: _cancellationToken);
    }

    public async Task RunAsync()
    {
        _cancellationToken = _cancellationTokenSource.Token;

        foreach (var gameType in GameManager.Games)
        {
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
        _cancellationTokenSource.Cancel();
        IsRunning = false;
    }

    private async Task OnUpdate(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        GameManager.RunningGames.ForEach(async instance => await instance.Game.DoUpdates(update));
        CommandManager.DoCommands(update);
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