using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Bots.CareBot;

public class AboutMeDatabase : CommonFileDatabase
{
    public AboutMeDatabase()
    {
        base.DatabaseFileName = "aboutme.db";
        base.DatabaseFilePath = "./db";
    }
}