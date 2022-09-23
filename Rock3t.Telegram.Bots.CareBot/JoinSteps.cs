namespace Rock3t.Telegram.Bots.CareBot;

public enum JoinSteps
{
    New = 0,
    PrivacyAccepted = 1,
    PrivacyRejected = -1,
    QuestionsAnswered = 2,
    RulesAccepted = 3,
    Joined = 10
}