using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rock3t.MetaProperties;
using Rock3t.Telegram.Lib.Commands;
using Rock3t.Telegram.Lib.Extensions;
using Rock3t.Telegram.Lib.Functions;
using Telegram.Bot;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rock3t.Telegram.Lib;



public abstract class TelegramBotBase : TelegramBotClient, ITelegramBot
{
    private CancellationToken _cancellationToken = CancellationToken.None;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    protected event EventHandler<Update>? ChatStarted;

    protected CommandManager CommandManager { get; }
    protected GameManager GameManager { get; }

    public List<IBotModule> Modules { get; }

    public IBotConfig Config { get; }

    public Message? LastMessage { get; private set; }

    public bool IsRunning { get; private set; }

    protected TelegramBotBase(string token, IBotConfig config, ILogger logger) : base(token)
    {
        Config = config;
        Modules = new List<IBotModule>();

        CommandManager = new CommandManager(this);
        CommandManager.AddAction("zufall", "Würfelt eine zahl zwischen 1 und 6", OnRandom);

        CommandManager.ChatStarted += OnChatStarted;
        GameManager = new GameManager();

        if (Config.ClearUpdatesOnStart)
        {
            var clearUpdatesTask = this.GetUpdatesAsync();

            clearUpdatesTask.Wait();
            var updates = clearUpdatesTask.Result;

            int? offset = null;

            if (updates.Length > 0)
            {

                logger.LogWarning("Missed updates: ");
                foreach (Update update in updates)
                {
                    offset = update.Id;
                    var jsonString = JsonConvert.SerializeObject(update, Formatting.Indented);
                    logger.LogWarning(jsonString);
                }

                this.GetUpdatesAsync(offset + 1).Wait();
            }
        }

    }

    protected virtual async Task OnRandom(Update update)
    {
        Message? message = update.GetUpdateMessage();

        if (message == null)
            return;

        await this.SendDiceAsync(message.Chat.Id, cancellationToken: _cancellationToken);
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
        GameManager.RunningGames.ForEach(async instance => await instance.Game.DoUpdates(update));
        await CommandManager.DoCommands(update);

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
                new ActionCommand(gameName.ToLower(), $"Spiele eine Runde {gameName}",
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
        update.SetMetaProperty(MetaProperties.Bot, this, true);
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