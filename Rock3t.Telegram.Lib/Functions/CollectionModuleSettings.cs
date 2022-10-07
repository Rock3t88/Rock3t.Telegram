using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Lib.Functions;

public class CollectionModuleSettings : IDatabaseEntity
{
    public long ChatId { get; set; }
    public Dictionary<string, int> ListMessageIds { get; set; }
    public Guid Id { get; set; }
}