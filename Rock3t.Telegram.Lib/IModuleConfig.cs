namespace Rock3t.Telegram.Lib;

public interface IModuleConfig
{
    string Name { get; }
    bool Enabled { get; }
}