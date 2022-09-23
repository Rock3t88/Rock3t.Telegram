using Rock3t.Telegram.Lib;
using Rock3t.Telegram.WebApi;

_DEBUG.SetDebugConfigPath("./_DEBUG/config/");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
    .ConfigureAppConfiguration((context, builder) =>
    {
        var path = _DEBUG.ConfigPath ?? "./config/appsettings.json";

        Console.WriteLine("config path: " + path);
        
        builder.AddJsonFile(path, false, true);
    })
    .Build();

App.Host = host;

await host.RunAsync();
