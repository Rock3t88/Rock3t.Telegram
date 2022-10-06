using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib;

public static class UserItemMessageExtensions
{
    public static bool ContainsMessageId(this Dictionary<long, List<int>> dict, Message? message)
    {
        if (message?.From?.Id == null)
            return false;

        return dict.ContainsKey(message.From.Id);
    }

    public static bool ContainsUserId(this Dictionary<long, List<int>> dict, Message? message)
    {
        if (message == null)
            return false;

        foreach (var values in dict.Values)
        {
            if (values.Contains(message.MessageId)) 
                return true;
        }

        return false;
    }
}