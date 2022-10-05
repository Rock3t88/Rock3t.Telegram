using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib.Commands;

public abstract class CommandBase : ICommand
{
    public abstract Func<Update, object[]?, Task<object?>> Command { get; }

    public string CommandString { get; }

    public string Name { get; }

    public string Description { get; }

    public async Task<object?> ExecuteAsync(Update update, params object[]? parameters)
    {
        return await Command.Invoke(update, parameters);
    }

    protected CommandBase(string commandName, string description)
    {
        Name = commandName;
        CommandString = $"/{commandName.ToLower()}";
        Description = description;
    }

    public override string ToString()
    {
        return CommandString;
    }
}