using Rock3t.Telegram.Lib;
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
    private ScaryTerryBotBase _botBase;

    public HomeAssistantWrapper(ScaryTerryBotBase botBase)
    {
        _botBase = botBase;
    }

    public async void CallService(ServicesTypes servicesType, MessageData data)
    {
        switch (servicesType)
        {
            case ServicesTypes.send_message:
                await _botBase.SendTextMessageAsync(data.target, data.message, ParseMode.Markdown);
                break;
            case ServicesTypes.send_photo:
                await _botBase.SendPhotoAsync(data.target, data.url, data.caption, ParseMode.Markdown);
                break;
            case ServicesTypes.notify:
                await _botBase.SendTextMessageAsync(_botBase.Config.AdminChannelId, data.message, ParseMode.Markdown);
                break;
            case ServicesTypes.send_voice:
                await _botBase.SendAudioAsync(data.target, data.url, data.caption, ParseMode.Markdown);
                break;
            case ServicesTypes.scene:
                await _botBase.SendTextMessageAsync(_botBase.Config.AdminChannelId, $"Scene executed: {data.entity_id}",
                    ParseMode.Markdown);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(servicesType), servicesType, null);
        }
    }
}