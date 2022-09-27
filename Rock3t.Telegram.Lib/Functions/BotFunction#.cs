namespace Rock3t.Telegram.Lib.Functions;

public class BotFunction
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public CommandManager CommandManager { get; set; }
    public GameManager GameManager { get; set; }

    public BotFunction(ITelegramBot bot)
    {
        CommandManager = new CommandManager(bot);
    }
}