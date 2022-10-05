using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib.Commands;

public interface ICommand
{
    string CommandString { get; }
    string Name { get; }
    string Description { get; }
    Func<Update, object[]?, Task<object?>> Command { get; }
    Task<object?> ExecuteAsync(Update update, params object[]? parameters);
}