using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Lib.Functions;

public class CollectionModuleItem : IDatabaseEntity
{
    public Guid Id { get; set; }
    public string Text { get; set; }

}