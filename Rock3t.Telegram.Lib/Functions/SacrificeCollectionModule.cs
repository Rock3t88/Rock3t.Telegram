namespace Rock3t.Telegram.Lib.Functions;

public sealed class SacrificeCollectionModule : CollectionModuleBase<CollectionModuleItem>
{
    public SacrificeCollectionModule(ITelegramBot bot, string name, string dbFilePath = "./db", string? dbFilename = null) 
        : base(bot, name, dbFilePath, dbFilename ?? $"{name}.db")
    {
        InitDefaultCommands();
    }

    protected override void InitDefaultCommands()
    {
        //CommandManager.AddAction("opfergaben", "Show list items", OnShowItems);
        CommandManager.AddAction<string>("opfergabe", "Opfergabe hinzufügen", OnAddListItems);
        CommandManager.AddAction<string>("opfergabe_entfernen", "Opfergabe entfernen", OnRemoveItems);
        //CommandManager.AddAction<string>("opfergabe_ändern", "Opfergabe ändern", OnUpdateItems);
    }
}