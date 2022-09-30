using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Bots.CareBot;

public class AboutMeEntity : IDatabaseEntity
{
    public Guid Id { get; set; }

    public long UserId { get; set; }
    public string? UserName { get; set; }

    public string? TelegramName { get; set; }
    public string? Name { get; set; }
    public string? Geburtsdatum { get; set; }
    public string? Geschlecht { get; set; }
    public string? Wohnort { get; set; }
    public string? Rolle { get; set; }
    public string? Privat_anschreiben_erlaubt { get; set; }
    public string? Andere_Plattformen { get; set; }

    public AboutMeEntity()
    {
        
    }

    public AboutMeEntity(long userId, string telegramName, string? userName)
    {
        UserId = userId;
        TelegramName = telegramName;
        UserName = userName;
    }
}