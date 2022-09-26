using YamlDotNet.Serialization;

namespace Rock3t.Telegram.Bots.CareBot;

public class AnswerOption
{
    [YamlIgnore] public static AnswerOption Yes { get; }
    [YamlIgnore] public static AnswerOption No { get; }
    [YamlIgnore] public static AnswerOption DontKnow { get; }

    [YamlIgnore] public int Id { get; }

    public string Text { get; }

    public AnswerOption(int id, string text)
    {
        Id = id;
        Text = text;
    }

    static AnswerOption()
    {
        Yes = new AnswerOption(-1, "Ja");
        No = new AnswerOption(-2, "Nein");
        DontKnow = new AnswerOption(-3, "Keine Ahnung");
    }
}