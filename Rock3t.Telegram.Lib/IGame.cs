using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib;

public interface IGame
{
    TelegramBotBase BotBase { get; }
    Message? LastMessage { get; set; }
    Message? LastAnswer { get; set; }
    bool Completed { get; }
    string Name { get; }
    Guid Id { get; }
    User Player { get; set; }
    Task DoUpdates(Update update);

    Task StartAsync(Update update);
}

public interface IGame<out T> : IGame where T : class
{
    T Model { get; }
}