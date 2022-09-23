using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib;

public class UpdatesEventArgs : EventArgs
{
    public Update Update { get; }
    public int? OffsetOld { get; }
    public int OffsetNew { get; set; }

    public UpdatesEventArgs(Update update, int? offsetOld, int? offsetNew = null)
    {
        Update = update;
        OffsetOld = offsetOld;
    }
}

public class GameUpdatesEventArgs : UpdatesEventArgs
{
    public IGame Game { get; } = null!;

    private GameUpdatesEventArgs(Update update, int? offsetOld, int? offsetNew = null) : base(update, offsetOld,
        offsetNew)
    {
    }

    public GameUpdatesEventArgs(IGame game, Update updates, int? offsetOld, int? offsetNew = null) :
        this(updates, offsetOld, offsetNew)
    {
        Game = game;
    }
}