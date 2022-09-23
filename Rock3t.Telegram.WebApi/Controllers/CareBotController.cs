using Microsoft.AspNetCore.Mvc;
using Rock3t.Telegram.Bots.CareBot;

namespace Rock3t.Telegram.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CareBotController : ControllerBase
{
    private CancellationTokenSource _cancellationTokenSource;

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

    [HttpGet("/start")]
    public bool Start()
    {
        var careBot = App.Host.Services.GetRequiredService<CareBot>();
        _careBotTask = careBot.RunAsync();
        return true;
    }

    [HttpGet("/stop")]
    public bool Stop()
    {
        _cancellationTokenSource.Cancel();
        return true;
    }
}