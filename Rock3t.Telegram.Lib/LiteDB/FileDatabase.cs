using System.Reflection;

namespace Rock3t.Telegram.Lib.LiteDB;

public interface IFileDatabase
{
    string DatabaseFullName { get; }
    string DatabaseFilePath { get; set; }
    string DatabaseFileName { get; set; }
    bool Initialize();
    T GetItem<T>(Guid id) where T : IDatabaseEntity;
    IEnumerable<T> GetItems<T>() where T : IDatabaseEntity;
    Guid InsertItem<T>(T item) where T : IDatabaseEntity;
    bool DeleteItem<T>(Guid id) where T : IDatabaseEntity;
    bool UpdateItem<T>(T item) where T : IDatabaseEntity;
}

public abstract class FileDatabase : IFileDatabase
{
    public string DatabaseFullName => Path.Combine(DatabaseFilePath, DatabaseFileName);

    public virtual string DatabaseFilePath { get; set; } =
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

    public virtual string DatabaseFileName { get; set; } =
        $"{Assembly.GetEntryAssembly()?.GetName().Name ?? Assembly.GetCallingAssembly().GetName().Name}.db";

    public virtual bool Initialize()
    {
        return true;
    }

    public abstract T GetItem<T>(Guid id) where T : IDatabaseEntity;
    public abstract IEnumerable<T> GetItems<T>() where T : IDatabaseEntity;
    public abstract Guid InsertItem<T>(T item) where T : IDatabaseEntity;
    public abstract bool DeleteItem<T>(Guid id) where T : IDatabaseEntity;
    public abstract bool UpdateItem<T>(T item) where T : IDatabaseEntity;
}