using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib.Commands;

public interface IActionCommand : ICommand
{
    Func<Update, Task> Action { get; }
    Task ExecuteAsync(Update update) => Action.Invoke(update);
}

public interface IActionCommand<in T> : ICommand
{
    Func<Update, T[], Task> Action { get; }
    Task ExecuteAsync(Update update, params T[] parameters) => Action.Invoke(update, parameters);
}