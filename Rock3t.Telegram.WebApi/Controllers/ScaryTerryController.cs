using Microsoft.AspNetCore.Mvc;
using Rock3t.Telegram.Bots.ScaryTerry;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Rock3t.Telegram.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ScaryTerryController : ControllerBase
{
    private readonly ILogger<ScaryTerryController> _logger = null!;
    private readonly IConfiguration _config = null!;

    private Task? _scaryTerryBotTask;

    private ScaryTerryController()
    {
    }

    public ScaryTerryController(ILogger<ScaryTerryController> logger, IConfiguration config) : this()
    {
        _logger = logger;
        _config = config;
    }

    [HttpGet("")]
    public ScaryTerryBot Get()
    {
        var scaryTerryBot = App.Host.Services.GetRequiredService<ScaryTerryBot>();
        return scaryTerryBot;
    }

    [HttpPost("say")]
    public async Task<Message?> Say(string text)
    {
        var scaryTerryBot = App.Host.Services.GetRequiredService<ScaryTerryBot>();
        return await scaryTerryBot.SendTextMessageAsync(scaryTerryBot.Config.MainChatId, text, ParseMode.Markdown);
    }

    [HttpGet("start")]
    public bool Start()
    {
        try
        {
            var scaryTerryBot = App.Host.Services.GetRequiredService<ScaryTerryBot>();

            if (scaryTerryBot.IsRunning)
                throw new InvalidOperationException("ScarayTerryBot is already running.");

            _scaryTerryBotTask = scaryTerryBot.RunAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return false;
        }
    }

    [HttpGet("stop")]
    public bool Stop()
    {
        try
        {
            var scaryTerryBot = App.Host.Services.GetRequiredService<ScaryTerryBot>();

            if (!scaryTerryBot.IsRunning)
                throw new InvalidOperationException($"{nameof(Stop)} - ScareTerryBot is not running!");

            scaryTerryBot.Stop();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return false;
        }
    }

    [HttpGet("reinitialize")]
    public bool Reinitialize()
    {
        try
        {
            var scaryTerryBot = App.Host.Services.GetRequiredService<ScaryTerryBot>();

            if (!scaryTerryBot.IsRunning)
                throw new InvalidOperationException($"{nameof(Stop)} - ScaryTerryBot is not running!");

            scaryTerryBot.Stop();
            _scaryTerryBotTask = scaryTerryBot.RunAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return false;
        }
    }
}