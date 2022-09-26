using Microsoft.Extensions.Logging;

namespace Rock3t.Telegram.Test.Console;

public class BotManager
{


    private ILogger<BotManager> _logger;

    public BotManager(ILogger<BotManager> logger)
    {
        _logger = logger;
    }
}