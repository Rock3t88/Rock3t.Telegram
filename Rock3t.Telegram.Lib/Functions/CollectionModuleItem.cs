using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Lib.Functions;

public class CollectionModuleItem : ITelegramCollectionEntity
{
    public Guid Id { get; set; }
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; }
    public string Value { get; set; }
    public string Text { get; set; }

}