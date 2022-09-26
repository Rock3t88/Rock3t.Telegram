using Rock3t.Telegram.Lib;

namespace Rock3t.Telegram.Bots.ScaryTerry.Config;

public class ScaryTerryConfig : BotConfig
{
    public string Title { get; set; }
    public long MainChatId { get; set; }
    public string DefaultWelcomeMessage { get; set; }
    public string DefaultMessage { get; set; }
    public string DefaultRecipientMessage { get; set; }
    public string RandomCommand { get; set; }
    public string AddWelcomeMessageCommand { get; set; }
    public List<Token> Tokens { get; set; } = new();
    public List<Action> Actions { get; set; } = new();
    public List<Action> RandomActions { get; set; } = new();
    public List<Notifier> Notifiers { get; set; } = new();
    public List<string> WelcomeMessages { get; set; } = new();

    public ScaryTerryConfig()
    {
    }

    //public static ScaryTerryConfig Load(string fileName = "Config\\Config.yml")
    //{
    //    string config = File.ReadAllText(fileName);

    //    var deserializer = new Deserializer();
    //    return deserializer.Deserialize<ScaryTerryConfig>(config);
    //}
}