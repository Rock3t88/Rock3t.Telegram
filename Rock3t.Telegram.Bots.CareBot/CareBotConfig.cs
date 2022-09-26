using Rock3t.Telegram.Lib;

namespace Rock3t.Telegram.Bots.CareBot;

public class CareBotConfig : BotConfig
{
    public List<Question> Questions { get; set; }

    public List<string> GroupRules { get; set; }
}