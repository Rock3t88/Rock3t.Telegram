namespace Rock3t.Telegram.Lib;

public abstract class ModuleConfigBase : IModuleConfig
{
    public string Name { get; set; }
    public bool Enabled { get; set; }

    //public T Module { get; set; }

    protected ModuleConfigBase()
    {
        
    }

    protected ModuleConfigBase(string name, bool enabled)
    {
        Name = name;
        Enabled = enabled;
    }
}

public class ModuleConfig : ModuleConfigBase
{
    public ModuleConfig()
    {

    }

    public ModuleConfig(string name, bool enabled) : base(name, enabled)
    {
        Name = name;
        Enabled = enabled;
    }
}

//public class ModuleConfig<T> : ModuleConfig where T : class, IModuleConfig, new()
//{
//    //public T? Object => Module as T;

//    public ModuleConfig()
//    {
        
//    }

//    public ModuleConfig(string name, bool enabled) : base(name, enabled)
//    {
//        Name = name;
//        Enabled = enabled;
//    }
//}