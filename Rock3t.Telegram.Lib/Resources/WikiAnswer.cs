namespace Rock3t.Telegram.Lib.Resources;

public class WikiAnswer
{
    public string Text { get; set; }
    public Uri? ImageUri { get; set; }
    public string? ImageCaption { get; set; }

    public WikiAnswer(string text)
    {
        Text = text;
    }
}