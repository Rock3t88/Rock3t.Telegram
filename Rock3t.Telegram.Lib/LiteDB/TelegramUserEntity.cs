using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rock3t.Telegram.Lib.LiteDB;

public class TelegramUserEntity : IDatabaseEntity, INotifyPropertyChanged
{
    private Guid _id;
    private string _name;
    private long _userId;
    private DateTime _timeCreated;
    private DateTime _lastUpdate;

    [Browsable(false)]
    public Guid Id
    {
        get => _id;
        set
        {
            if (value.Equals(_id)) return;
            _id = value;
            OnPropertyChanged();
        }
    }


    [ReadOnly(true)]
    public long UserId
    {
        get => _userId;
        set
        {
            if (value == _userId) return;
            _userId = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public DateTime TimeCreated
    {
        get => _timeCreated;
        set
        {
            if (value == _timeCreated) return;
            _timeCreated = value;
            OnPropertyChanged();
        }
    }

    public DateTime LastUpdate
    {
        get => _lastUpdate;
        set
        {
            if (value == _lastUpdate) return;
            _lastUpdate = value;
            OnPropertyChanged();
        }
    }

    public TelegramUserEntity()
    {
        TimeCreated = DateTime.Now;
        LastUpdate = DateTime.Now;
    }

    public override bool Equals(object? obj)
    {
        if (obj is TelegramUserEntity entity)
            return Equals(entity);
        else
            return false;
    }

    protected bool Equals(TelegramUserEntity other)
    {
        return _userId == other._userId;
    }

    public override int GetHashCode()
    {
        return _userId.GetHashCode();
    }

    //public override int GetHashCode()
    //{
    //    unchecked
    //    {
    //        var hashCode = _id.GetHashCode();
    //        hashCode = (hashCode * 397) ^ _userId.GetHashCode();
    //        return hashCode;
    //    }
    //}

    public TelegramUserEntity Clone()
    {
        return (TelegramUserEntity)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{Name}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}