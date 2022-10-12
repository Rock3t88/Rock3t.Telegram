namespace Rock3t.Telegram.Lib;

public sealed class RandomTalkModuleConfig : ModuleConfig
{
    public List<string> Poems { get; set; }
    public List<string> Facts { get; set; }

    public RandomTalkModuleConfig()
    {
        
    }

    public RandomTalkModuleConfig(string name, bool enabled) : base(name, enabled)
    {

    }
}