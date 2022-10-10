// See https://aka.ms/new-console-template for more information

using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.SearchConsole.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rock3t.Telegram.Bots.CareBot;
using Rock3t.Telegram.Bots.ScaryTerry;
using Rock3t.Telegram.Bots.ScaryTerry.Config;
using Rock3t.Telegram.Lib;
using Rock3t.Telegram.Test.Console;
using Rock3t.Telegram.WebApi;
using Telegram.Bot;

//TelegramBotClient client = new TelegramBotClient(new TelegramBotClientOptions("5426505698:AAEDXPSt4Uw-BRsP-eZpU_Lzd9kb6tBYUgc"));
//await client.SendTextMessageAsync(-1001840042949, "Test");
//return;
//var cred = 
//    OAuth2Authenticator.Authenticate("google-cred.json", new[]
//    {
//        SearchConsoleService.Scope.Webmasters
//    });

//using var service = new DiscoveryService(new BaseClientService.Initializer
//{
//    HttpClientInitializer = cred
//});

//service.
//var sites = service.Sites.List().Execute();

return;
_DEBUG.SetDebugConfigPath("./_DEBUG/config/");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, builder) =>
    {
        var path = _DEBUG.ConfigPath ?? "./config/appsettings.json";

#if DEBUG
        Console.WriteLine("config path: " + path);
#endif

        builder.AddJsonFile(path, false, true);

    }).ConfigureServices((context, services) =>
    {
        //services.AddSingleton<ScaryTerryBot>();
        services.AddSingleton<CareBot>();

        foreach (var botConfig in context.Configuration.GetSection("Bots").GetChildren())
        {
            var name = botConfig.GetSection("Name").Value;

            if (name.ToLower().Equals("scaryterry"))
            {
                services.Configure<ScaryTerryConfig>(botConfig);
            }
            else if (name.ToLower().Equals("carebot"))
            {
                services.Configure<CareBotConfig>(botConfig);
            }
            else
            {
                services.Configure<BotConfig>(botConfig);
            }
        }
    })
    .Build();

App.Host = host;
var careBot = host.Services.GetRequiredService<CareBot>();
await careBot.RunAsync();

