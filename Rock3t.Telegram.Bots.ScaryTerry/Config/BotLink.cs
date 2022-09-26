namespace Rock3t.Telegram.Bots.ScaryTerry.Config;

public class BotLink
{
    public string Name { get; set; }
    public string Url { get; set; }

    public BotLink(string name, string url)
    {
        Name = name;
        Url = url;
    }

    public BotLink()
    {
        
    }
}