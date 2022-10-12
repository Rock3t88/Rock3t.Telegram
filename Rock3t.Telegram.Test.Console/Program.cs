// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Rock3t.Telegram.Bots.CareBot;
using Rock3t.Telegram.Bots.ScaryTerry;
using Rock3t.Telegram.Bots.ScaryTerry.Config;
using Rock3t.Telegram.Lib;
using Rock3t.Telegram.Lib.LiteDB;
using Rock3t.Telegram.Lib.Resources;
using Rock3t.Telegram.Test.Console;

CommonFileDatabase fileDatabase = new CommonFileDatabase();
fileDatabase.DatabaseFilePath = "./";
fileDatabase.DatabaseFileName = "ScarryTerry.db";

var items = fileDatabase.GetItems<StringEntity>();
var items1 = fileDatabase.GetItems<StringEntity>().Where(x => x.Name?.Equals("SentPoems") == true).ToList();
var items2 = fileDatabase.GetItems<StringEntity>().Where(x => x.Name?.Equals("SentFacts") == true).ToList();


//var rabbitItem = rabbitEntity.FirstOrDefault();

var lines = await File.ReadAllLinesAsync("Resources\\german-nouns.json");

var allCategories = new Dictionary<string, CategoryItem>();
var allNouns = new Dictionary<string, NounEntity>();

return;
//foreach (string line in lines)
//{
//    dynamic? deserialized = JsonConvert.DeserializeObject(line);
//    dynamic? senses = deserialized?.senses;
//    dynamic? categories = senses?[0].categories;

//    var tmpCategories = new List<CategoryItem>();

//    if (categories != null)
//    {
//        foreach (dynamic item in categories)
//        {
//            string categoryName = item.name.ToString();
//            if (allCategories.ContainsKey(categoryName))
//            {
//                tmpCategories.Add(allCategories[categoryName]);
//            }
//            else
//            {
//                Console.WriteLine($"New Category: " + categoryName);

//                string kind = item.kind.ToString();

//                var categoryItem = new CategoryItem(categoryName, kind);
//                allCategories.Add(categoryName, categoryItem);
//                tmpCategories.Add(categoryItem);
//            }
//        }
//    }

//    string word = deserialized?.word;
//    string lang = deserialized?.lang;

//    Console.WriteLine("New Word:" + word);

//    IEnumerable<string> tags = deserialized?.tags;
//    IEnumerable<string> glosses = deserialized?.glosses;
//    var noun = 
//        new NounEntity(word, lang, tmpCategories);

//    //fileDatabase.InsertItem(noun);

//    if (!allNouns.ContainsKey(word))
//        allNouns.Add(word, noun);
//}

//Console.WriteLine();
////});


//Wiki wiki = new Wiki();
//string result = await wiki.SearchAsync("Bot");
//return;

//_DEBUG.SetDebugConfigPath("./_DEBUG/config/");

//var host = Host.CreateDefaultBuilder(args)
//    .ConfigureAppConfiguration((context, builder) =>
//    {
//        var path = _DEBUG.ConfigPath ?? "./config/appsettings.json";

//#if DEBUG
//        Console.WriteLine("config path: " + path);
//#endif

//        builder.AddJsonFile(path, false, true);

//    }).ConfigureServices((context, services) =>
//    {
//        //services.AddSingleton<ScaryTerryBot>();
//        services.AddSingleton<CareBot>();

//        foreach (var botConfig in context.Configuration.GetSection("Bots").GetChildren())
//        {
//            var name = botConfig.GetSection("Name").Value;

//            if (name.ToLower().Equals("scaryterry"))
//            {
//                services.Configure<ScaryTerryConfig>(botConfig);
//            }
//            else if (name.ToLower().Equals("carebot"))
//            {
//                services.Configure<CareBotConfig>(botConfig);
//            }
//            else
//            {
//                services.Configure<BotConfig>(botConfig);
//            }
//        }
//    })
//    .Build();

//App.Host = host;
//var careBot = host.Services.GetRequiredService<CareBot>();
//await careBot.RunAsync();

