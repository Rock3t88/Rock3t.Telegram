using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib.Commands;

public class ActionCommand : CommandBase, IActionCommand
{
    public Func<Update, Task> Action { get; }

    public override Func<Update, object[]?, Task<object?>> Command => async (update, _) =>
    {
        await Action.Invoke(update);
        return typeof(void);
    };

    public ActionCommand(string commandName, string description, Func<Update, Task> action) : base(commandName, description)
    {
        Action = action;
    }
}

public class ActionCommand<T> : CommandBase, IActionCommand<T>
{
    public Func<Update, T[], Task> Action { get; }

    public override Func<Update, object[]?, Task<object?>> Command => async (update, parameters) =>
    {
        await Action.Invoke(update, (parameters ?? throw new ArgumentNullException(nameof(parameters))).Cast<T>().ToArray());
        return Task.CompletedTask;
    };

    public ActionCommand(string commandName, string description, Func<Update, T[], Task> action) : base(commandName, description)
    {
        Action = action;
    }
}