using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.VisualBasic;
using Rock3t.Telegram.Lib.Commands;
using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib.Functions;

public interface IBotModule
{
    Guid Id { get; }
    string Name { get; }
    
    CommandManager CommandManager { get; }
    IReadOnlyCollection<IBotModule> SubModules { get; }
}

public abstract class BotModuleBase : IBotModule
{
    private readonly Dictionary<string, IBotModule> _subModules;

    public ITelegramBot Bot { get; }
    public abstract Guid Id { get; }
    public string Name { get; } 

    public CommandManager CommandManager { get; }

    public IReadOnlyCollection<IBotModule> SubModules => _subModules.Values.ToImmutableList();

    protected virtual void AddSubModule(IBotModule module)
    {
        string name = module.Name.ToLower();

        if (!_subModules.ContainsKey(name))
        {
            _subModules.Add(name, module);
        }
    }

    protected virtual void RemoveSubModule(string name)
    {
        string key = name.ToLower();

        if (_subModules.ContainsKey(key))
        {
            _subModules.Remove(key);
        }
    }

    protected BotModuleBase(ITelegramBot bot, string name) : this(name, bot)
    {
    }

    private BotModuleBase(string name, ITelegramBot bot)
    {
        _subModules = new Dictionary<string, IBotModule>();

        CommandManager = new CommandManager(bot);
        Name = name;
        Bot = bot;
    }
}