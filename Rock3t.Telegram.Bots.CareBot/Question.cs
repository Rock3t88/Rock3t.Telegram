namespace Rock3t.Telegram.Bots.CareBot;

public class Question
{
    public string Text { get; set; } = null!;
    public List<AnswerOption> AnswerOptions { get; set; }

    public Question()
    {
        AnswerOptions = new List<AnswerOption>();
    }

    public Question(string text) : this()
    {
        Text = text;
    }

    public override string ToString()
    {
        return Text;
    }
}