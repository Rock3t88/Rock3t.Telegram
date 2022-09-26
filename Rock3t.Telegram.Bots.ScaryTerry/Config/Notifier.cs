namespace Rock3t.Telegram.Bots.ScaryTerry.Config;

public class Notifier
{
    public long Id { get; set; }
    public string Name { get; set; }


    public Notifier(long id, string name)
    {
        Id = id;
        Name = name;
    }

    public Notifier()
    {
    }
}