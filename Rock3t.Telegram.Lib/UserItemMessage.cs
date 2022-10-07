namespace Rock3t.Telegram.Lib;

public class UserItemMessage
{
    public ITelegramBot Bot { get; }
    public long UserId { get; }
    public int MessageId { get; }
    public long ChatId { get; }
    public Guid? EntityId { get; set; }

    public UserItemMessage(ITelegramBot bot, long chatId, long userId, int messageId)
    {
        Bot = bot;
        ChatId = chatId;
        MessageId = messageId;
        UserId = userId;
    }
}