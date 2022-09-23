using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib;

public class Command
{
    public string CommandString { get; }

    public string Name { get; }

    public Func<Update, Task> Action { get; }

    public string Description { get; }

    public async Task ExecuteAsync(Update update)
    {
        await Action.Invoke(update);
    }

    public Command(string command, string description, Func<Update, Task> action)
    {
        Name = command;
        CommandString = command.ToLower();
        Description = description;
        Action = action;
    }
}