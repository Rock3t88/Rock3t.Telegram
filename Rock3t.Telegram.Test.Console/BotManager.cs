﻿using Microsoft.Extensions.Logging;
using Rock3t.Telegram.Lib;

namespace Rock3t.Telegram.Test.Console;

public class BotManager
{
    private ILogger<BotManager> _logger;
    private List<TelegramBotBase> _bots;


    public BotManager(ILogger<BotManager> logger)
    {
        _bots = new List<TelegramBotBase>();
        _logger = logger;
    }
}