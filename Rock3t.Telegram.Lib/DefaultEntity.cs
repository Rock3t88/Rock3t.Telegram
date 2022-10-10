using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Lib;

public class DefaultEntity<T> : IDatabaseEntity where T : class, new()
{
    public Guid Id { get; set; }
    public T Value { get; set; } = new T();
}