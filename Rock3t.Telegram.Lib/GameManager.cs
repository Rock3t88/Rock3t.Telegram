using System.Collections.Immutable;
using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib;

public class GameManager
{
    private readonly List<Type> _games = new();
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

    public IReadOnlyList<Type> Games => ImmutableList.CreateRange(_games);

    public GameManager()
    {
    }

    public void Add<T>() where T : class, IGame
    {
        Type gameType = typeof(T);
        
        if (!_games.Contains(gameType))
            _games.Add(gameType);
    }

    public void Remove<T>() where T : class, IGame
    {
        Type gameType = typeof(T);

        if (_games.Contains(gameType))
            _games.Remove(gameType);
    }

    public IGame Create(Type gameType, User user, TelegramBot bot)
    {
        //game = game.ToLower();

        if (!_games.Contains(gameType))
            throw new Exception($"The game {gameType.Name} does not exist!");

        if (PlayingUsers.Contains(user))
            throw new Exception(
                $"{user.Username} is already playing {RunningGames.First(instance => instance.User.Equals(user))}!");

        if (!UserMapping.ContainsKey(user.Id))
            UserMapping.Add(user.Id, user);

        //Type gameType = Games[game];

        var newGame = Activator.CreateInstance(gameType, bot) as IGame;

        if (newGame == null)
            throw new Exception($"Could not create IGame instance of type {gameType.Name}!");

        newGame.Player = user;

        var gameInstance = new GameInstance(newGame, user, null);

        RunningGames.Add(gameInstance);

        return newGame;
    }
}