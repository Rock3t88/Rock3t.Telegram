﻿namespace Rock3t.Telegram.Lib;

public interface IBotConfig
{
    string Name { get; }
    string Token { get; set; }
    long AdminChannelId { get; set; }
    long FoyerChannelId { get; set; }
    long MainChatId { get; set; }
    bool ClearUpdatesOnStart { get; set; }
    List<string> AdminUsers { get; set; }
}

public class BotConfig : IBotConfig
{
    //public string Name { get; set; }

    public virtual string Name
    {
        get
        {
            var name = GetType().Name.Replace("BotConfig", "");

            if (!name.ToLower().EndsWith("bot")) name += "Bot";

            return name;
        }
    }

    public long MainChatId { get; set; }
    public bool ClearUpdatesOnStart { get; set; }
    public List<string> AdminUsers { get; set; }
    public string Token { get; set; }
    public long AdminChannelId { get; set; }
    public long FoyerChannelId { get; set; }
}