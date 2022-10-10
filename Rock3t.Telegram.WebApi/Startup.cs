using Rock3t.Telegram.Bots.CareBot;
using Rock3t.Telegram.Bots.ScaryTerry;
using Rock3t.Telegram.Bots.ScaryTerry.Config;
using Rock3t.Telegram.Lib;

namespace Rock3t.Telegram.WebApi;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddSingleton<CareBot>();
        services.AddSingleton<ScaryTerryBot>();

        foreach (var botConfig in Configuration.GetSection("Bots").GetChildren())
        {
            var name = botConfig.GetSection("Name").Value;

            if (name.ToLower().Equals("carebot"))
                services.Configure<CareBotConfig>(botConfig);
            else if (name.ToLower().Equals("scaryterry"))
                services.Configure<ScaryTerryConfig>(botConfig);
            else
                services.Configure<BotConfig>(botConfig);
        }
        //var app = builder.Build();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Configure the HTTP request pipeline.
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(builder => { builder.MapControllers(); });

        //app.Run();
    }
}