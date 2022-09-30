using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib;

public class ParameterCommand : Command
{
    public ParameterCommand(string command, string description, Func<Update, Task> action) : base(command, description, action)
    {
    }

}