using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Bots.Orgabot;

public class TodoItem : IDatabaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Owner { get; set; }
    public string? Responsible { get; set; }
    public string Text { get; set; }
    public bool Done { get; set; }
    public DateTime? UntilDateTime { get; set; }
}