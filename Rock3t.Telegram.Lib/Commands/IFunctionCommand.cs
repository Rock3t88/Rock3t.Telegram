using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib.Commands;

public interface IFunctionCommand<T> : ICommand
{
    Func<Update, Task<T?>> Function { get; }
    Task<T?> ExecuteAsync(Update update, params T[] parameters) => Function.Invoke(update);
}

public interface IFunctionCommand<in T, TO> : ICommand
{
    Func<Update, T[], Task<TO?>> Function { get; }
    Task<TO?> ExecuteAsync(Update update, params T[] parameters) => Function.Invoke(update, parameters);
}