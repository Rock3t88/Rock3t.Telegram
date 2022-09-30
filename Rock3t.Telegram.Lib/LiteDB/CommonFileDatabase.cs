using LiteDB;

namespace Rock3t.Telegram.Lib.LiteDB;

public class CommonFileDatabase : FileDatabase
{
    private string _databaseFilePath;

    public override string DatabaseFilePath
    {
        get => _databaseFilePath;
        set
        {
            if (Equals(_databaseFilePath, value))
                return;

            if (!Directory.Exists(value))
                Directory.CreateDirectory(value);

            _databaseFilePath = value;
        }
    }

    public override bool DeleteItem<T>(Guid id)
    {
        using (var db = new LiteDatabase(DatabaseFullName))
        {
            var templateCol = db.GetCollection<T>();
            return templateCol.Delete(id);
        }
    }

    public override bool UpdateItem<T>(T item)
    {
        using (var db = new LiteDatabase(DatabaseFullName))
        {
            var templateCol = db.GetCollection<T>();
            return templateCol.Update(item);
        }
    }

    public override T GetItem<T>(Guid id)
    {
        using (var db = new LiteDatabase(DatabaseFullName))
        {
            var templateCol =
                db.GetCollection<T>();

            return
                templateCol.Query().Where(
                    e => e.Id.Equals(id)).FirstOrDefault();
        }
    }

    public override Guid InsertItem<T>(T item)
    {
        using (var db = new LiteDatabase(DatabaseFullName))
        {
            var templateCol =
                db.GetCollection<T>();

            return templateCol.Insert(item);
        }
    }

    public override IEnumerable<T> GetItems<T>()
    {
        using (var db = new LiteDatabase(DatabaseFullName))
        {
            var itemCollection = db.GetCollection<T>();
            return itemCollection.Query().ToArray();
        }
    }

    public ILiteQueryable<T> Query<T>()
    {
        using (var db = new LiteDatabase(DatabaseFullName))
        {
            var itemCollection = db.GetCollection<T>();
            return itemCollection.Query();
        }
    }
}