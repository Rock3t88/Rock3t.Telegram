namespace Rock3t.Telegram.Lib.LiteDB;

public interface ITelegramCollectionEntity : IDatabaseEntity
{
    Guid Id { get; set; }
    long ChatId { get; set; }
    long UserId { get; set; }
    string UserName { get; set; }
    string Value { get; set; }
}

public interface IDatabaseEntity
{
    Guid Id { get; set; }
}