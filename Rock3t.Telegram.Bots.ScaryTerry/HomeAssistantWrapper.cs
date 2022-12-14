using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Rock3t.Telegram.Bots.ScaryTerry;

public class MessageData
{
    public string message { get; set; }
    public long target { get; set; }
    public string url { get; set; }
    public string caption { get; set; }
    public string title { get; set; }
    public string entity_id { get; set; }
}

public enum ServicesTypes
{
    send_message,
    send_photo,
    notify,
    send_voice,
    scene
}


public class HomeAssistantWrapper
{
   
    private ScaryTerryBot _bot;

    public HomeAssistantWrapper(ScaryTerryBot bot)
    {
        _bot = bot;
    }

    public async void CallService(ServicesTypes servicesType, MessageData data)
    {
        switch (servicesType)
        {
            case ServicesTypes.send_message:
                await _bot.SendTextMessageAsync(data.target, data.message, ParseMode.Markdown);
                break;
            case ServicesTypes.send_photo:
                await _bot.SendPhotoAsync(data.target, data.url, data.caption, ParseMode.Markdown);
                break;
            case ServicesTypes.notify:
                await _bot.SendTextMessageAsync(_bot.Config.AdminChannelId, data.message, ParseMode.Markdown);
                break;
            case ServicesTypes.send_voice:
                await _bot.SendAudioAsync(data.target, data.url, data.caption, ParseMode.Markdown);
                break;
            case ServicesTypes.scene:
                await _bot.SendTextMessageAsync(_bot.Config.AdminChannelId, $"Scene executed: {data.entity_id}", ParseMode.Markdown);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(servicesType), servicesType, null);
        }
    }
}