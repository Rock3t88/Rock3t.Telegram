using Rock3t.Telegram.Lib.Commands;
using Telegram.Bot.Types;

public class FunctionCommand<T> : CommandBase, IFunctionCommand<T>
{
    public Func<Update, Task<T?>> Function { get; }

    public override Func<Update, object[]?, Task<object?>> Command => async (update, _) => await Function.Invoke(update);

    public FunctionCommand(string commandName, string description, Func<Update, Task<T?>> function) : base(commandName, description)
    {
        Function = function;
    }
}

public class FunctionCommand<T, TO> : CommandBase, IFunctionCommand<T, TO>
{
    public Func<Update, T[], Task<TO?>> Function { get; }

    public override Func<Update, object[]?, Task<object?>> Command => async (update, parameters) =>
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        return await Function.Invoke(update, parameters.Cast<T>().ToArray());
    };

    public FunctionCommand(string commandName, string description, Func<Update, T[]?, Task<TO?>> command) : base(commandName, description)
    {
        Function = command;
    }
}