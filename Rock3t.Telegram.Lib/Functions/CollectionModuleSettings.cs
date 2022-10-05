using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Lib.Functions;

public class CollectionModuleSettings : IDatabaseEntity
{
    public long ChatId { get; set; }
    public int? ListMessageId { get; set; }
    public Guid Id { get; set; }
}