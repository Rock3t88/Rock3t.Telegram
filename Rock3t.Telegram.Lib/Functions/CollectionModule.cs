using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib.Functions;

public abstract class CollectionModuleBase<T> : BotModuleBase
{
    protected virtual Collection<T> InternalCollection { get; }

    public IReadOnlyCollection<T> Collection => InternalCollection.ToImmutableList();

    public override Guid Id => Guid.NewGuid();

    protected CollectionModuleBase(ITelegramBot bot, string name) : base(bot, name)
    {
        InternalCollection = new Collection<T>();
    }

    protected virtual void InitCommands()
    {
        CommandManager.Add("add", "Add list item", OnAddListItem);
        CommandManager.Add("show", "Show list items", OnShowItems);
        CommandManager.Add("remove", "Remove list item", OnRemoveItem);
    }

    protected virtual async Task OnAddListItem(Update update)
    {
        await Task.CompletedTask;
    }

    protected virtual async Task OnShowItems(Update update)
    {
        await Task.CompletedTask;
    }

    protected virtual async Task OnRemoveItem(Update update)
    {
        await Task.CompletedTask;
    }

    private void Add(T item)
    {
        if (!InternalCollection.Contains(item))
            InternalCollection.Add(item);
    }

    private void Remove(T item)
    {
        if (InternalCollection.Contains(item))
            InternalCollection.Remove(item);
    }

    private void Remove(int index)
    {
        if (index < 0 || index >= InternalCollection.Count)
            return;

        InternalCollection.RemoveAt(index);
    }
}