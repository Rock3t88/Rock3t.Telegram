namespace Rock3t.Telegram.Bots.ScaryTerry.Config;

public class Token
{
    public string Key { get; set; }
    public string Value { get; set; }

    public Token(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public Token()
    {
    }
}