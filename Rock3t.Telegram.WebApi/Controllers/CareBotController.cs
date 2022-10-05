using Microsoft.AspNetCore.Mvc;
using Rock3t.Telegram.Bots.CareBot;
using Rock3t.Telegram.Bots.ScaryTerry;

namespace Rock3t.Telegram.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CareBotController : ControllerBase
{
    private readonly ILogger<CareBotController> _logger = null!;
    private readonly IConfiguration _config = null!;

    private Task? _careBotTask;

    private CareBotController()
    {
    }

    public CareBotController(ILogger<CareBotController> logger, IConfiguration config) : this()
    {
        _logger = logger;
        _config = config;
    }

    [HttpGet("")]
    public CareBot Get()
    {
        var careBot = App.Host.Services.GetRequiredService<CareBot>();
        return careBot;
    }

    [HttpGet("start")]
    public bool Start()
    {
        try
        {
            var careBot = App.Host.Services.GetRequiredService<ScaryTerryBot>();

            if (careBot.IsRunning)
                throw new InvalidOperationException("CareBot is already running.");

            //ToDo!!! careBot.Initialize();
            _careBotTask = careBot.RunAsync();

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
            var careBot = App.Host.Services.GetRequiredService<ScaryTerryBot>();

            if (!careBot.IsRunning)
                throw new InvalidOperationException($"{nameof(Stop)} - CareBot is not running!");

            careBot.Stop();

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
            var careBot = App.Host.Services.GetRequiredService<ScaryTerryBot>();

            if (!careBot.IsRunning)
                throw new InvalidOperationException($"{nameof(Stop)} - CareBot is not running!");

            careBot.Stop();
            //ToDo!!! careBot.Initialize();
            _careBotTask = careBot.RunAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return false;
        }
    }
}