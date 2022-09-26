using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib;

public class CommandManager
{
    public event EventHandler<Update> ChatStarted;

    public TelegramBot Bot { get; }

    public Dictionary<string, Command> Commands = new();

    public CommandManager(TelegramBot bot)
    {
        Bot = bot;
        Commands.Add("hilfe", new Command("hilfe", "Listet alle Commands auf.", GetHelp));
        Commands.Add("start", new Command("start", "Startet einen Chat mit Carebot.", OnChatStart));
    }

    private Task OnChatStart(Update update)
    {
        ChatStarted?.Invoke(this, update);
        return Task.CompletedTask;
    }

    public void DoCommands(Update update)
    {
        var text = update.Message?.Text;

        if (string.IsNullOrWhiteSpace(text))
            return;

        var cmdRegex = new Regex(@"/.*", RegexOptions.IgnoreCase);

        var match = cmdRegex.Match(text);

        if (match.Success)
        {
            var cmdEndIndex = match.Value.IndexOf(' ');

            if (cmdEndIndex <= 0)
                cmdEndIndex = match.Value.Length;

            var cmd = match.Value.Substring(1, cmdEndIndex - 1).ToLower();

            if (Commands.ContainsKey(cmd))
            {
                var retValue = Commands[cmd].ExecuteAsync(update);
                Console.WriteLine(retValue);
            }
        }
    }

    private async Task GetHelp(Update update)
    {
        var chatId = Bot.GetChatId(update);

        var sb = new StringBuilder();

        foreach (var item in Commands) sb.AppendLine($"/{item.Key} - {item.Value.Description}");

        await Bot.SendMessage(chatId, sb.ToString());
    }
}