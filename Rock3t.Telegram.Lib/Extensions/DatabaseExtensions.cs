using LiteDB;
using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Lib.Extensions;

public static class DatabaseExtensions
{
    public static T? GetItemByName<T>(this CommonFileDatabase db, string name) where T : INamedDatabaseEntity
        => db.Query<T>().ToArray().FirstOrDefault(); //.Where(entity => entity.Name!.ToLower().Equals(name.ToLower())).FirstOrDefault();

    public static T[] GetItemsByName<T>(this CommonFileDatabase db, string name) where T : INamedDatabaseEntity
        => db.Query<T>().ToArray();//.Where(entity => entity.Name!.ToLower().Equals(name.ToLower()));

}