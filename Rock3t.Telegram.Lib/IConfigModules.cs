namespace Rock3t.Telegram.Lib;

public interface IHasModuleConfig<out T>
{
    T Module { get; }
}