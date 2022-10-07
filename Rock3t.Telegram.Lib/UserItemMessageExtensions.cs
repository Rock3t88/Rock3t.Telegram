using Telegram.Bot;
using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib;

public static class UserItemMessageExtensions
{
    public static bool ContainsMessageId(this List<UserItemMessage> list, int messageId)
    {
        return list.FirstOrDefault(item => item.MessageId.Equals(messageId)) != null;
    }

    public static bool ContainsUserId(this List<UserItemMessage> list, long userId)
    {
        return list.FirstOrDefault(item => item.UserId.Equals(userId)) != null;
    }

    public static bool ContainsEntityId(this List<UserItemMessage> list, Guid entityId)
    {
        return list.FirstOrDefault(item => item.EntityId.Equals(entityId)) != null;
    }

    public static bool Add(this List<UserItemMessage> list, ITelegramBot bot, Message message, Guid? entityId = null)
    {
        if (message.From?.Id == null)
            return false;

        list.Add(new UserItemMessage(bot, message.Chat.Id, message.From.Id, message.MessageId)
        {
            EntityId = entityId
        });

        return true;
    }

    public static UserItemMessage? ItemByUserId(this List<UserItemMessage> list, long? userId)
    {
        return list.FirstOrDefault(item => item.UserId.Equals(userId));
    }

    public static UserItemMessage? ItemByMessageId(this List<UserItemMessage> list, int? messageId)
    {
        return list.FirstOrDefault(item => item.MessageId.Equals(messageId));
    }

    public static UserItemMessage? ItemByEntityId(this List<UserItemMessage> list, Guid? entityId)
    {
        return list.FirstOrDefault(item => item.EntityId.Equals(entityId));
    }

    public static async Task<bool> RemoveByMessageId(this List<UserItemMessage> list, int? messageId)
    {
        if (messageId == null)
            return false;

        if (list.ContainsMessageId((int)messageId))
        {
            UserItemMessage[] tmpList = list.ToArray();

            foreach (var item in tmpList.Where(item => item.MessageId.Equals(messageId)))
            {
                await item.Bot.DeleteMessageAsync(item.ChatId, item.MessageId);
                list.Remove(item);
            }
            return true;
        }

        return false;
    }

    public static async Task<bool> RemoveByUserId(this List<UserItemMessage> list, long? userId)
    {
        if (userId == null) 
            return false;

        if (list.ContainsUserId((long)userId))
        {
            UserItemMessage[] tmpList = list.ToArray();

            foreach (var item in tmpList.Where(item => item.UserId.Equals(userId)))
            {
                await item.Bot.DeleteMessageAsync(item.ChatId, item.MessageId);
                list.Remove(item);
            }
            return true;
        }

        return false;
    }

    public static async Task<bool> RemoveByEntityId(this List<UserItemMessage> list, Guid? entityId)
    {
        if (entityId == null)
            return false;

        if (list.ContainsEntityId((Guid)entityId))
        {
            UserItemMessage[] tmpList = list.ToArray();

            foreach (var item in tmpList.Where(item => item.EntityId.Equals(entityId)))
            {
                await item.Bot.DeleteMessageAsync(item.ChatId, item.MessageId);
                list.Remove(item);
            }
            return true;
        }

        return false;
    }

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