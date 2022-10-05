using System.Collections.ObjectModel;
using Rock3t.Telegram.Lib.LiteDB;
using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib.Functions;

public class DbCollectionModule : CollectionModuleBase<IDatabaseEntity>
{
    private CommonFileDatabase _db;


    public DbCollectionModule(ITelegramBot bot, string name) : base(bot, name)
    {
        _db = new CommonFileDatabase();
    }

    protected Task OnAddListItem(Update update)
    {
        return Task.CompletedTask;  
        //return base.OnAddListItem(update);
    }
}