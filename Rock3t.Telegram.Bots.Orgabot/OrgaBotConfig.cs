using Rock3t.Telegram.Lib;

namespace Rock3t.Telegram.Bots.Orgabot;

public class OrgaBotConfig : BotConfig
{
    public List<TodoItem> Todo { get; set; }

    public OrgaBotConfig()
    {
        Todo = new List<TodoItem>();
    }
} 