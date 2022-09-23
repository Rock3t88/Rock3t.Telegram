using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib;

public class GameManager
{
    private readonly List<GameInstance> _runningGames = new();

    public List<GameInstance> RunningGames
    {
        get
        {
            _runningGames.RemoveAll(instance => instance.Game.Completed);

            return _runningGames;
        }
    }

    public IEnumerable<User> PlayingUsers => _runningGames.Select(instance => instance.User);

    public Dictionary<long, User> UserMapping { get; } = new();
    public List<Type> Games { get; } = new();

    public GameManager()
    {
    }

    public IGame Create(Type gameType, User user, TelegramBotBase botBase)
    {
        //game = game.ToLower();

        if (!Games.Contains(gameType))
            throw new Exception($"The game {gameType.Name} does not exist!");

        if (PlayingUsers.Contains(user))
            throw new Exception(
                $"{user.Username} is already playing {RunningGames.First(instance => instance.User.Equals(user))}!");

        if (!UserMapping.ContainsKey(user.Id))
            UserMapping.Add(user.Id, user);

        //Type gameType = Games[game];

        var newGame = Activator.CreateInstance(gameType, botBase) as IGame;

        if (newGame == null)
            throw new Exception($"Could not create IGame instance of type {gameType.Name}!");

        newGame.Player = user;

        var gameInstance = new GameInstance(newGame, user, null);

        RunningGames.Add(gameInstance);

        return newGame;
    }
}