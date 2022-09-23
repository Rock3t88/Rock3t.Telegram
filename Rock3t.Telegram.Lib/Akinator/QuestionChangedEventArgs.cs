using Akinator.Api.Net.Enumerations;
using Akinator.Api.Net.Model;

namespace Rock3t.Telegram.Lib.Akinator;

public class QuestionChangedEventArgs : EventArgs
{
    public AkinatorQuestion Question { get; }
    public AnswerOptions Answer { get; set; } = AnswerOptions.Unknown;

    public QuestionChangedEventArgs(AkinatorQuestion question)
    {
        Question = question;
    }
}