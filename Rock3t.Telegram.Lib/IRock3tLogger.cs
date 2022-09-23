using Serilog;

namespace Rock3t.Telegram.Lib;

public interface IRock3tLogger : ILogger
{
    public void Write(string message);
}