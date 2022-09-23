using Rock3t.Telegram.Lib;

namespace Rock3t.Telegram.Bots.CareBot;

public class ChatSecrets
{
    public static List<string> Values { get; }

    static ChatSecrets()
    {
        Values = new List<string>(File.ReadAllLines(_DEBUG.WordlistPath ?? "./config/wordlist.txt"));
    }
}