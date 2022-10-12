using System.Net.Sockets;
using Rock3t.Telegram.Lib.LiteDB;

namespace Rock3t.Telegram.Lib.Resources;

public class NounEntity : IDatabaseEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Language { get; set; } = null!;

    public List<string> Tags { get; set; } = new();

    public List<string> Glosses { get; set; } = new();

    public List<CategoryItem> Categories { get; set; } = new();

    public NounEntity()
    {
        
    }

    public NounEntity(string name, string language, IEnumerable<CategoryItem> categories)
    {
        Name = name;
        Language = language;
        //Tags.AddRange(tags);
        //Glosses.AddRange(glosses);
        //IEnumerable<string> tags, IEnumerable<string> glosses, 
        Categories.AddRange(categories);
    }
}