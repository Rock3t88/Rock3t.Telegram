using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rock3t.Telegram.Lib.LiteDB;

public class TelegramMessageEntity : IDatabaseEntity, INotifyPropertyChanged
{
    private Guid _id;
    private DateTime _timeCreated;
    private DateTime _lastUpdate;
    private string? _message;
    private string _type;
    private bool _triggered;

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

    public bool Triggered
    {
        get => _triggered;
        set
        {
            if (value.Equals(_triggered)) return;
            _triggered = value;
            OnPropertyChanged();
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            if (value == _message) return;
            _message = value;
            OnPropertyChanged();
        }
    }

    public string Tag
    {
        get => _type;
        set
        {
            if (value == _type) return;
            _type = value;
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

    public TelegramMessageEntity()
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

    protected bool Equals(TelegramMessageEntity other)
    {
        return _message == other._message;
    }

    public override int GetHashCode()
    {
        return _message?.GetHashCode() ?? -1;
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

    public TelegramMessageEntity Clone()
    {
        return (TelegramMessageEntity)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{Message}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}