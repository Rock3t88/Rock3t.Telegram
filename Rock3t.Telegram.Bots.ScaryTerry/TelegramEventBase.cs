namespace Rock3t.Telegram.Bots.ScaryTerry;

public abstract class TelegramEventBase
{
    public int id { get; set; }
    public long chat_id { get; set; }
    public long user_id { get; set; }
    public global::Telegram.Bot.Types.User? from { get; set; }
}