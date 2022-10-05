//using System.Collections.Immutable;
//using System.Collections.ObjectModel;

//namespace Rock3t.Telegram.Lib.Functions;

//public class BotModuleManager
//{
//    private readonly Collection<Type> _modules;
//    private readonly Dictionary<Type, IBotModule> _instances;

//    public IReadOnlyCollection<Type> Modules => ImmutableList.CreateRange(_modules);

//    public CommandManager CommandManager { get; }
//    public GameManager GameManager { get; }

//    public void AddAction<T>() where T : class, IBotModule
//    {
//        Type moduleType = typeof(T);

//        if (!_modules.Contains(moduleType))
//            _modules.AddAction(moduleType);
//    }

//    public void Remove<T>() where T : class, IBotModule
//    {
//        Type moduleType = typeof(T);

//        if (_modules.Contains(moduleType))
//            _modules.Remove(moduleType);
//    }

//    public void InitializeModules()
//    {
//        _instances.Clear();

//        foreach (Type module in _modules)
//        {
//            IBotModule instance = (IBotModule)Activator.CreateInstance(module)!;

//            foreach (KeyValuePair<string, CommandBase> commandBase in instance.Commands)
//            {
//                CommandManager.Commands.AddAction(commandBase.Key, commandBase.Value);
//            }

//            foreach (Type game in instance.Games)
//            {
//                GameManager.AddAction<AkinatorGame>();
//            }

//            _instances.AddAction(module, instance);
//        }
//    }

//    public BotModuleManager(ITelegramBot bot)
//    {
//        CommandManager = new CommandManager(bot);
//        GameManager = new GameManager();

//        _instances = new Dictionary<Type, IBotModule>();
//        _modules = new Collection<Type>();
//    }
//}