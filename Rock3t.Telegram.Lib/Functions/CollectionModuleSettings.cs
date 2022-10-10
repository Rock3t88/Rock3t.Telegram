using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Lib.Functions;

public class CollectionModuleSettings : IDatabaseEntity
{
    public long ChatId { get; set; }
    public DefaultEntity<Dictionary<long, int>> ListMessageChatIdToMessageId { get; set; } = new();
    public Guid Id { get; set; }
}