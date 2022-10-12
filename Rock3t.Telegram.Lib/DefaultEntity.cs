using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Lib;

public class DefaultEntity<T> : INamedDatabaseEntity where T : class, new()
{
    public Guid Id { get; set; }
    public T Value { get; set; } = new T();
    public string? Name { get; set; } = null;
}

public class StringEntity : INamedDatabaseEntity
{
    public Guid Id { get; set; }
    public string Value { get; set; }
    public string? Name { get; set; }
}