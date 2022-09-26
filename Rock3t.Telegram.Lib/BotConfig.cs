namespace Rock3t.Telegram.Lib;

public interface IBotConfig
{
    string Name { get; }
    string Token { get; set; }
    long AdminChannelId { get; set; }
    long FoyerChannelId { get; set; }
    bool ClearUpdatesOnStart { get; set; }
    List<string> AdminUsers { get; set; }
}

public class BotConfig : IBotConfig
{
    public virtual string Name
    {
        get
        {
            var name = GetType().Name.Replace("BotConfig", "");

            if (!name.ToLower().EndsWith("botBase")) name += "BotBase";

            return name;
        }
    }

    public bool ClearUpdatesOnStart { get; set; }
    public List<string> AdminUsers { get; set; }
    public string Token { get; set; }
    public long AdminChannelId { get; set; }
    public long FoyerChannelId { get; set; }
}