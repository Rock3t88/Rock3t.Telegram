using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Bots.ScaryTerry.db;

public class ScaryTerryDb : CommonFileDatabase
{
    public ScaryTerryDb()
    {
        base.DatabaseFileName = "ScaryTerry.db";
        base.DatabaseFilePath = "./db";
    }

    public Guid CreateUser(long userId, string name)
    {
        return base.InsertItem(new TelegramUserEntity { UserId = userId, Name = name });
    }

    public Guid AddWelcomeMessage(string message, bool triggered = false)
    {
        return base.InsertItem(new TelegramMessageEntity
            { Message = message, Tag = "WelcomeMessage", Triggered = triggered });
    }

    public List<string> GetWelcomeMessages(bool? triggered = null)
    {
        if (triggered == null)
            return base.GetItems<TelegramMessageEntity>().Where(entity => entity.Tag.Equals("WelcomeMessage"))
                .Select(entity => entity.Message).ToList();
        else
            return base.GetItems<TelegramMessageEntity>()
                .Where(entity => entity.Tag.Equals("WelcomeMessage") && entity.Triggered == triggered)
                .Select(entity => entity.Message).ToList();
    }

    public TelegramUserEntity[] GetUsers()
    {
        return base.GetItems<TelegramUserEntity>().ToArray();
    }
}