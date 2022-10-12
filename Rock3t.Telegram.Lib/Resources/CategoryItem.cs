namespace Rock3t.Telegram.Lib.Resources;

public class CategoryItem
{
    public string Name { get; set; } = null!;

    public string? Kind { get; set; }

    public List<string> Parents { get; set; } = new();

    public CategoryItem()
    {
        
    }

    public CategoryItem(string name, string? kind)
    {
        Name = name;
        Kind = kind;
    }
}