using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib;

public class GameInstance
{
    public Task? Task { get; set; }
    public IGame Game { get; set; }
    public User User { get; set; }

    public GameInstance(IGame game, User user, Task? task)
    {
        Game = game;
        User = user;
        Task = task;
    }
}