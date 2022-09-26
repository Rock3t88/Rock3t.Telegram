namespace Rock3t.Telegram.Bots.ScaryTerry.Config;

public class Action
{
    public string Name { get; set; }
    public AudioService? AudioService { get; set; }
    public SceneService? SceneService { get; set; }
    public MessageService? MessageService { get; set; }
    public AkinatorService? AkinatorService { get; set; }

    public Action(string name)
    {
        Name = name;
    }

    public Action()
    {
    }
}