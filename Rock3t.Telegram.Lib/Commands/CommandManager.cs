using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib.Commands;

public class CommandManager
{
    public event EventHandler<Update> ChatStarted;

    public ITelegramBot Bot { get; }

    public Dictionary<string, CommandBase> Commands = new();

    public CommandManager(ITelegramBot bot)
    {
        Bot = bot;
        Commands.Add("hilfe", new ActionCommand("hilfe", "Listet alle Commands auf.", GetHelp));
        Commands.Add("start", new ActionCommand("start", "Startet einen Chat mit Carebot.", OnChatStart));
    }

    private Task OnChatStart(Update update)
    {
        ChatStarted?.Invoke(this, update);
        return Task.CompletedTask;
    }

    //public void AddAction(string cmd, string description, Func<Update, Task> func)
    //{
    //    if (!Commands.ContainsKey(cmd.ToLower()))
    //    {
    //        CommandBase commandBase = new CommandBase(cmd.ToLower(), description, func);
    //        Commands.AddAction(cmd.ToLower(), commandBase);
    //    }
    //}
    public void AddAction(string cmd, string description, Func<Update, Task> func)
    {
        if (!Commands.ContainsKey(cmd.ToLower()))
        {
            CommandBase commandBase = new ActionCommand(cmd.ToLower(), description, func);
            Commands.Add(cmd.ToLower(), commandBase);
        }
    }
    public void AddAction<T>(string cmd, string description, Func<Update, T[], Task> func)
    {
        if (!Commands.ContainsKey(cmd.ToLower()))
        {
            CommandBase commandBase = new ActionCommand<T>(cmd.ToLower(), description, func);
            Commands.Add(cmd.ToLower(), commandBase);
        }
    }

    public async Task<object?> DoCommands(Update update)
    {
        var text = update.Message?.Text;

        if (string.IsNullOrWhiteSpace(text))
            await Task.CompletedTask;

        var cmdRegex = new Regex(@"\/(?<command>\S+)( )?(?<value>.*)?", RegexOptions.IgnoreCase);

        var match = cmdRegex.Match(text);

        if (match.Success)
        {
            string cmd = match.Groups["command"].Value;
            string value = match.Groups["value"].Value;

            if (Commands.ContainsKey(cmd))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    var result = await Commands[cmd].ExecuteAsync(update);
                    Console.WriteLine(result);
                    return result;
                }
                else
                {
                    var result = await Commands[cmd].ExecuteAsync(update, value);
                    Console.WriteLine(result);
                    return result;
                }
            }
        }

        return Task.CompletedTask;
    }

    private async Task GetHelp(Update update)
    {
        var chatId = Bot.GetChatId(update);

        var sb = new StringBuilder();

        foreach (var item in Commands) sb.AppendLine($"/{item.Key} - {item.Value.Description}");

        await Bot.SendMessage(chatId, sb.ToString());
    }
}