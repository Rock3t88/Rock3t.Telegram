using Serilog.Events;

namespace Rock3t.Telegram.Lib;

public class Logger : IRock3tLogger
{
    public void Write(LogEvent logEvent)
    {
        Console.WriteLine(logEvent.RenderMessage());
    }

    public void Write(string message)
    {
        Console.WriteLine(message);
    }

    //public void LogInfo(string message, LogEventLevel level)
    //{
    //    Write(new LogEvent(DateTime.Now, level, null, new MessageTemplate("{0}", new[] { new TextToken(message) }), ));
    //}
}