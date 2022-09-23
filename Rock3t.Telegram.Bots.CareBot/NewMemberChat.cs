using System.Text;
using Rock3t.Telegram.Lib;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace Rock3t.Telegram.Bots.CareBot;

public class NewMemberChat
{
    private CareBot _careBot;

    public bool WaitingForAnswer { get; private set; }

    public long Id { get; }
    public long UserId { get; }
    public string UserName { get; }
    public string? StartSecret { get; private set; }
    public bool RulesAccepted { get; private set; }
    public bool Accepted { get; set; }
    public ChatInviteLink? InviteLink { get; private set; }
    public JoinSteps CurrentJoinStep { get; private set; }
    public Question? CurrentQuestion { get; private set; }
    public List<Question> Questions { get; }

    public void SetSecret(string secret)
    {
        if (StartSecret == null)
            StartSecret = secret;
        else
            throw new Exception("Secret already set!");
    }

    public NewMemberChat(long id, long userId, string userName, CareBot careBot, params Question[] questions)
    {
        _careBot = careBot;
        Questions = new List<Question>(questions);

        Id = id;
        UserId = userId;
        UserName = userName;
    }

    public async Task Execute(Update update)
    {
        var text = update.Message?.Text;

        if (string.IsNullOrWhiteSpace(text))
            return;

        await _careBot.SendChatActionAsync(update.Message.Chat.Id, ChatAction.Typing);
        Thread.Sleep(2000);

        switch (CurrentJoinStep)
        {
            case JoinSteps.New:

                if (WaitingForAnswer)
                {
                    if (text.ToLower().Equals("einverstanden"))
                    {
                        CurrentJoinStep = JoinSteps.PrivacyAccepted;
                        await OnSendGroupRules(update);
                    }
                    else if (text.ToLower().Equals("Ich habe doch kein Interesse mehr"))
                    {
                        //CurrentJoinStep = JoinSteps.PrivacyRejected;
                    }

                    WaitingForAnswer = false;
                }
                else
                {
                    await OnStartChat(this, update);
                    WaitingForAnswer = true;
                }

                break;
            case JoinSteps.PrivacyAccepted:

                if (RulesAccepted || text.ToLower().Equals(StartSecret?.ToLower()))
                {
                    RulesAccepted = true;
                    CurrentJoinStep = JoinSteps.PrivacyAccepted;
                    int questionIndex;

                    if (CurrentQuestion == null)
                    {
                        questionIndex = 0;

                        await _careBot.SendTextMessageAsync(_careBot.AdminChannelId,
                            $"Die Regeln wurden von @{UserName} akzeptiert.");
                        await _careBot.SendTextMessageAsync(
                            update.Message.Chat.Id,
                            "Nun stelle ich dir noch ein paar vorbereitende Fragen, " +
                            "durch die deine Aufnahme noch schneller durchgeführt werden kann.\r\n\r\nDu musst aber natürlich auf keine der Fragen antworten, wenn du das nicht möchtest. " +
                            "Schreibe in diesem Fall einfach irgendetwas in den Chat, z.B. _keine Angabe_ oder _ka_.\r\n\r\n" +
                            "*Beachte: Orga-Mitglieder können deine Antworten maximal 7 Tage lang sehen, danach werden sie automatisch gelöscht.*",
                            ParseMode.Markdown);
                    }
                    else
                    {
                        questionIndex = Questions.IndexOf(CurrentQuestion) + 1;
                    }

                    if (WaitingForAnswer)
                        await _careBot.SendTextMessageAsync(_careBot.AdminChannelId,
                            $"*Antwort von @{UserName}*\r\n\r\n*{CurrentQuestion.Text}*\r\n{text}", ParseMode.Markdown);

                    if (questionIndex >= Questions.Count)
                    {
                        WaitingForAnswer = false;
                        CurrentJoinStep = JoinSteps.QuestionsAnswered;
                        await OnSendJoinLink(update);
                        CurrentJoinStep = JoinSteps.Joined;
                        return;
                    }

                    CurrentQuestion = Questions[questionIndex];
                    await _careBot.SendChatActionAsync(update.Message.Chat.Id, ChatAction.Typing);
                    Thread.Sleep(2000);
                    await OnAskQuestion(update, questionIndex);
                    WaitingForAnswer = true;
                }

                break;
            case JoinSteps.PrivacyRejected:
                break;
            case JoinSteps.QuestionsAnswered:
                break;
            case JoinSteps.RulesAccepted:

                if (update.Type == UpdateType.ChatJoinRequest)
                {
                    CurrentJoinStep = JoinSteps.Joined;

                    await _careBot.SendTextMessageAsync(update.Message.Chat.Id,
                        "Klasse! Das hat ja super geklappt! ☺️\r\n" +
                        "Nun wird sich sobald wie möglich ein Orga-Mitglied bei dir melden. Bitte habe etwas Geduld, " +
                        "Möglicherweise sind gerade alle beschäftigt. Solltest du dennoch das Gefühl bekommen vergessen worden zu sein, " +
                        "schreibe *ping* in diesen Chat.", ParseMode.Markdown);
                }

                break;
            case JoinSteps.Joined:

                if (text.ToLower().Contains("ping"))
                {
                    await _careBot.SendTextMessageAsync(update.Message.From.Id,
                        $"Alles klar, ich habe die Orga-Mitglieder darüber informiert, dass du ungeduldigt bist.");
                    await _careBot.SendTextMessageAsync(_careBot.AdminChannelId,
                        $"@{update.Message.From.Username} hat gepingt.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task OnAskQuestion(Update update, int questionIndex)
    {
        await _careBot.SendTextMessageAsync(update.Message.Chat.Id, Questions[questionIndex].Text);
    }

    private async Task OnSendGroupRules(Update update)
    {
        var firstName = update.Message.Chat.FirstName;
        var lastName = update.Message.Chat.LastName;
        var userName = update.Message.Chat.Username;

        await _careBot.SendTextMessageAsync(_careBot.AdminChannelId, $"Neue Gruppenanfrage von @{userName}");

        Thread.Sleep(3000);

        var random = new Random();
        var word = ChatSecrets.Values[random.Next(0, ChatSecrets.Values.Count)];

        SetSecret(word);

        var groupRules = File.ReadAllText(_DEBUG.GroupRulesPath ?? "./config/gruppenregeln.txt");
        groupRules = string.Format(groupRules, word);

        await _careBot.SendTextMessageAsync(update.Message.Chat.Id, groupRules, ParseMode.Markdown);

        Thread.Sleep(2000);

        var sb = new StringBuilder();
        sb.AppendLine(
            "Na wunderbar, das freut mich doch sehr! 😊\r\n\r\n" +
            "Als nächstes bitte ich dich unsere Gruppenregeln zu bestätigen. *Bitte lies sie dir sorgfältig durch*, " +
            "darin ist nämlich auch der nächste Schritt beschrieben, ohne den es nicht weiter geht. 😉");

        await _careBot.SendTextMessageAsync(update.Message.Chat.Id, sb.ToString(), ParseMode.Markdown);
    }

    private async Task OnSendJoinLink(Update update)
    {
        var userName = update.Message.Chat.Username;

        Thread.Sleep(2000);

        var sb = new StringBuilder();
        sb.AppendLine("Bravo! Du hast es fasst geschafft! 🥳");
        sb.AppendLine();
        sb.AppendLine(
            "Als nächstes schicke ich dir einen Link für den Zutritt in unser Foyer. " +
            "Beachte dass dieser Link nur *für dich 24 Stunden gültig* ist und genau *ein Mal* benutzt werden kann. " +
            "Danach wird sich ein Orga-Mitglied sobald wie möglich bei dir melden.");

        await _careBot.SendTextMessageAsync(update.Message.Chat.Id, sb.ToString(), ParseMode.Markdown);


        var linkTime = DateTime.Now;
        linkTime = linkTime.AddHours(24);

        var inviteLink =
            await _careBot.CreateChatInviteLinkAsync(
                _careBot.FoyerChannelId, "CG/L - NRW - Foyer", linkTime);

        InviteLink = inviteLink;

        await _careBot.SendTextMessageAsync(update.Message.Chat.Id, $"{inviteLink.InviteLink}", ParseMode.Markdown);
    }

    private async Task OnStartChat(object? sender, Update update)
    {
        Thread.Sleep(2000);

        var firstName = update.Message.Chat.FirstName;
        var lastName = update.Message.Chat.LastName;
        var userName = update.Message.Chat.Username;

        var sb = new StringBuilder();
        sb.AppendLine(string.Format("Hallo {0}! Freut mich sehr, dass du mir schreibst ☺️",
            firstName ?? userName ?? lastName));
        sb.AppendLine();
        sb.AppendLine(
            "Bei uns wird *Vertrauen* sehr groß geschrieben, deswegen möchte ich dir kurz erklären was " +
            "hier mit deinen Daten passiert. Denn schließlich bist du gewohnt, " +
            "dass nur die eine Person deine Nachrichten lesen kann, mit der du schreibst. " +
            "\r\n\r\n" +
            "Nunja, vermutlich hast du es bereits an meinem Namen erkannt. Ich bin ein Bot. Das heißt, " +
            "dass mindestens die EntwicklerInnen die Nachrichten aus diesem Chat mitlesen könnten, " +
            "wenn sie wollten. Da bei uns *Datenschutz* großgeschrieben wird, werden deine Nachrichten daher erst weitergeleitet " +
            "sobald du deine Zustimmung dazu gegeben hast.\r\n" +
            "_Um Transparenz und Datenschutz zu wahren, besteht sogar die Möglichkeit in meinen Quellcode zu schauen: https://github.com/Rock3t88/Rock3t.Telegram/_" +
            "\r\n\r\n" +
            "Um deine Gruppenaufnahme durchführen zu können, bitte ich dich nun zu *Bestätigen, " +
            "dass du diese Information verstanden hast und damit einverstanden bist, dass die Orga-Mitglieder von CG/L - NRW " +
            "über dein Interesse an der Gruppenaufnahme informiert werden und Zugriff auf die Nachrichten bekommen, " +
            "die du in diesem Chat mit mir schreibst.*");


        await _careBot.SendTextMessageAsync(update.Message.Chat.Id, sb.ToString(),
            ParseMode.Markdown, replyMarkup: new ReplyKeyboardMarkup(
                new List<KeyboardButton>
                {
                    new("Einverstanden"),
                    new("Ich habe doch kein Interesse mehr")
                })
            {
                OneTimeKeyboard = true
            });
    }
}