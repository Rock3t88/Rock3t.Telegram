using Microsoft.Extensions.Logging;
using Rock3t.Telegram.Lib;
using Rock3t.Telegram.Lib.LiteDB;
using Telegram.Bot.Types;

namespace Rock3t.Telegram.Bots.Orgabot;

public class Orgabot : TelegramBot
{
    private readonly OrgaBotConfig _config;
    private readonly CommonFileDatabase _database;

    public Orgabot(string token, ILogger<Orgabot> logger, OrgaBotConfig config) : base(token, logger)
    {
        _database = new CommonFileDatabase();
        _database.DatabaseFilePath = "./db";
        _database.DatabaseFileName = "./orgabot.db";

        _config = config;
        CommandManager.Commands.Add("todo", new Command(
            "todo", "/todo {name} {responsible} {until} {text}", OnAddTodo));
    }

    private async Task OnAddTodo(Update update)
    {
        var from = update.Message?.From;

        if (from?.Username == null)
            return;

        if (_config.AdminUsers.Contains(from.Username))
        {
            var todoItem = new TodoItem();
            todoItem.Owner = from.Username;
            todoItem.UntilDateTime = DateTime.Now.AddDays(7);
            todoItem.Text = "Test Todo item " + Random.Shared.Next(10, 1000000);
            todoItem.Responsible = todoItem.Owner;
            
            // Todo add orga item to db
            _database.InsertItem(todoItem);
        }

        await Task.CompletedTask;
    }

    protected override Task OnUpdate(Update update)
    {
        return base.OnUpdate(update);
    }

    protected override async Task OnChatAccepted(Update update)
    {
        await Task.CompletedTask;
    }
}