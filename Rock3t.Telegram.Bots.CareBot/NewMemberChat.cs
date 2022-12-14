using System.Text;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rock3t.Telegram.Bots.CareBot;

public class NewMemberChat
{
    private CareBot _careBot;
    private readonly ILogger _logger;
    static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

    public User User { get; }

    public bool WaitingForAnswer { get; private set; }
    public long Id { get; }
    public long UserId { get; }
    public string? UserName { get; }
    public string Name { get; }
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

    public NewMemberChat(long id, User user, CareBot careBot, params Question[] questions)
    {
        _careBot = careBot;
        _logger = careBot.Logger;
        
        Questions = new List<Question>(questions);

        Id = id;
        User = user;
        UserId = user.Id;
        UserName = user.Username;
        Name = $"{user.FirstName}{(user.LastName == null ? "" : $" {user.LastName}")} (@{UserName ?? "<unbekannt>"})";
    }

    public async Task Execute(Update update)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();

            var text = update.Message?.Text;

            if (string.IsNullOrWhiteSpace(text))
                return;

            long? chatId = update.Message?.Chat.Id;

            _logger.LogDebug(
                "[{chatId}] {class}.{method}: {currentJoinStep} - {user} ({userId}, {startSecret})\r\nWaitingForAnswer: {waitingForAnswer}\r\n" +
                "ChatAccepted: {chatAccepted}\r\nRulesAccpeted: {rulesAccepted}\r\nCurrentQuestion: {currentQuestion}\r\nInviteLink: {inviteLink}",
                chatId, nameof(NewMemberChat), nameof(Execute), CurrentJoinStep, update.Message?.From?.Username,
                update.Message?.From?.Id,
                StartSecret, WaitingForAnswer, Accepted, RulesAccepted, CurrentQuestion, InviteLink?.InviteLink);

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
                            _logger.LogDebug("[{chatId}] {method}", chatId, nameof(OnSendGroupRules));
                            await OnSendGroupRules(update);
                        }
                        else if (text.ToLower().Equals("Ich habe doch kein Interesse mehr"))
                        {
                            _logger.LogDebug("[{chatId}] {method} Kein Intersse!", chatId, nameof(OnSendGroupRules));
                            //CurrentJoinStep = JoinSteps.PrivacyRejected;
                        }

                        WaitingForAnswer = false;
                    }
                    else
                    {
                        _logger.LogDebug("[{chatId}] {method}", chatId, nameof(OnSendGroupRules));
                        await OnStartChat(this, update);
                        WaitingForAnswer = true;
                    }

                    break;
                case JoinSteps.PrivacyAccepted:

                    if (!RulesAccepted && !text.ToLower().Equals(StartSecret?.ToLower()))
                    {
                        _logger.LogDebug("[{chatId}] {method}", chatId, "WrongSecret");

                        await _careBot.SendTextMessageAsync(
                            update.Message.Chat.Id,
                            "Das war leider falsch, hast du die Regeln etwa nicht sorgfältig genug gelesen?\t\n" +
                            "Es ist wirklich ganz einfach, du musst sie einmal richtig lesen, dann weißt du sofort wie es weiter geht 😉");
                        return;
                    }

                    if (RulesAccepted || text.ToLower().Equals(StartSecret?.ToLower()))
                    {
                        RulesAccepted = true;
                        CurrentJoinStep = JoinSteps.PrivacyAccepted;
                        int questionIndex;

                        if (CurrentQuestion == null)
                        {
                            questionIndex = 0;

                            await _careBot.SendTextMessageAsync(_careBot.AdminChannelId,
                                $"Die Regeln wurden von {Name} akzeptiert.");

                            _logger.LogDebug("[{chatId}] {method}", chatId, "QuestionsStartet");

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
                                $"*Antwort von {Name}*\r\n\r\n*{CurrentQuestion.Text}*\r\n{text}",
                                ParseMode.Markdown);

                        if (questionIndex >= Questions.Count)
                        {
                            WaitingForAnswer = false;
                            CurrentJoinStep = JoinSteps.QuestionsAnswered;

                            _logger.LogDebug("[{chatId}] {method}", chatId, nameof(OnSendJoinLink));

                            await OnSendJoinLink(update);
                            CurrentJoinStep = JoinSteps.Joined;
                            return;
                        }

                        CurrentQuestion = Questions[questionIndex];
                        await _careBot.SendChatActionAsync(update.Message.Chat.Id, ChatAction.Typing);
                        Thread.Sleep(2000);

                        _logger.LogDebug("[{chatId}] {method}({questionIndex})", chatId, nameof(OnAskQuestion),
                            questionIndex);

                        await OnAskQuestion(update, questionIndex);
                        WaitingForAnswer = true;
                    }

                    break;
                case JoinSteps.PrivacyRejected:
                    _logger.LogDebug("[{chatId}] PrivacyRejected", chatId);
                    break;
                case JoinSteps.QuestionsAnswered:
                    _logger.LogDebug("[{chatId}] - QuestionAnswered", chatId);
                    break;
                case JoinSteps.RulesAccepted:

                    if (update.Type == UpdateType.ChatJoinRequest)
                    {
                        CurrentJoinStep = JoinSteps.Joined;

                        _logger.LogDebug("[{chatId}] - Finalized", chatId);

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
                        _logger.LogDebug("[{chatId}] - Ping", chatId);

                        await _careBot.SendTextMessageAsync(update.Message.From.Id,
                            $"Alles klar, ich habe die Orga-Mitglieder darüber informiert, dass du ungeduldigt bist.");
                        await _careBot.SendTextMessageAsync(_careBot.AdminChannelId,
                            $"{Name} hat gepingt.");
                    }
                    else
                    {
                        await _careBot.SendTextMessageAsync(_careBot.AdminChannelId,
                            $"*Nachricht von {Name}:*\n{update.Message.Text}");
                    }

                    break;
                default:
                    _logger.LogDebug("[{chatId}] - Wrong JoinStep", chatId);
                    throw new ArgumentOutOfRangeException();
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        finally
        {
            _semaphoreSlim.Release();
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

        await _careBot.SendTextMessageAsync(_careBot.AdminChannelId, $"Neue Gruppenanfrage von {Name}");

        Thread.Sleep(3000);

        var random = new Random();
        var word = ChatSecrets.Values[random.Next(0, ChatSecrets.Values.Count)];

        _logger.LogDebug("{metodName}({word})", nameof(SetSecret), word);

        SetSecret(word);

        StringBuilder groupRulesBuilder = new StringBuilder();
        groupRulesBuilder.AppendLine("*Gruppenregeln*");

        foreach (var rule in _careBot.Config.GroupRules)
        {
            groupRulesBuilder.AppendLine();
            groupRulesBuilder.AppendLine(rule);
        }

        string groupRules = groupRulesBuilder.ToString().Replace("#secret#", word);

        await _careBot.SendTextMessageAsync(update.Message.Chat.Id, groupRules, ParseMode.Markdown);

        Thread.Sleep(2000);

        var sb = new StringBuilder();
        sb.AppendLine(
            "Na wunderbar, das freut mich doch sehr! 😊\r\n\r\n" +
            "Als nächstes bitte ich dich unsere Gruppenregeln zu bestätigen. *Bitte lies sie dir sorgfältig durch*, " +
            "darin ist nämlich auch der nächste Schritt beschrieben, ohne den es nicht weiter geht. 😉");

        await _careBot.SendTextMessageAsync(update.Message.Chat.Id, sb.ToString(), ParseMode.Markdown);
        _logger.LogDebug("GroupRules sent");

    }

    private async Task OnSendJoinLink(Update update)
    {
        Thread.Sleep(2000);

        var sb = new StringBuilder();
        sb.AppendLine("Bravo! Du hast es fasst geschafft! 🥳");
        sb.AppendLine();
        sb.AppendLine("Als nächstes wird sich ein Orga-Mitglied zeitnah bei dir melden.");
        //sb.AppendLine(
        //    "Als nächstes schicke ich dir einen Link für den Zutritt in unser Foyer. " +
        //    "Beachte dass dieser Link nur *für dich 24 Stunden gültig* ist und genau *ein Mal* benutzt werden kann. " +
        //    "Danach wird sich ein Orga-Mitglied sobald wie möglich bei dir melden.");

        await _careBot.SendTextMessageAsync(update.Message.Chat.Id, sb.ToString(), ParseMode.Markdown);

        //var linkTime = DateTime.Now;
        //linkTime = linkTime.AddHours(24);

        //var inviteLink =
        //    await _careBot.CreateChatInviteLinkAsync(
        //        _careBot.FoyerChannelId, "CG/L - NRW - Foyer", linkTime);

        //InviteLink = inviteLink;

        //await _careBot.SendTextMessageAsync(update.Message.Chat.Id, $"Bitteschön: {inviteLink.InviteLink}", ParseMode.Markdown);

        //_logger.LogDebug("Link sent: {inviteLink}", inviteLink.InviteLink);
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

        if (string.IsNullOrWhiteSpace(userName))
        {
            sb.AppendLine();
            sb.AppendLine("Zuerst einmal ist mir aufgefallen, dass du keinen Benutzernamen erstellt hast.");
            sb.AppendLine("Leider ist das nötig, damit wir dich am Ende des Aufnahmeprozesses der Gruppe hinzufügen können.");
            sb.AppendLine("Eine Anleitung dazu findest du in der Google-Suche: https://www.google.com/search?q=telegram+benutzername+erstellen");
        }

        sb.AppendLine();
        sb.AppendLine(
            "Bei uns wird *Vertrauen* sehr groß geschrieben, deswegen möchte ich dir kurz erklären was " +
            "hier mit deinen Daten passiert. Denn schließlich bist du gewohnt, " +
            "dass nur die eine Person deine Nachrichten lesen kann, mit der du schreibst. " +
            "\r\n\r\n" +
            "Nunja, vermutlich hast du es bereits an meinem Namen erkannt. Ich bin ein Bot. Das heißt, " +
            "dass mindestens die EntwicklerInnen die Nachrichten aus diesem Chat mitlesen könnten, " +
            "wenn sie wollten. Da bei uns *Datenschutz* großgeschrieben wird, werden deine Nachrichten daher erst weitergeleitet " +
            "sobald du deine Zustimmung dazu gegeben hast." +
            "\r\n\r\n" +
            "Um deine Gruppenaufnahme durchführen zu können, bitte ich dich nun zu *Bestätigen, " +
            "dass du diese Information verstanden hast und damit einverstanden bist, dass die Orga-Mitglieder von CG/L - NRW " +
            "über dein Interesse an der Gruppenaufnahme informiert werden und Zugriff auf die Nachrichten bekommen, " +
            "die du in diesem Chat mit mir schreibst.*");


        await _careBot.SendTextMessageAsync(update.Message.Chat.Id, sb.ToString(),
            ParseMode.Markdown, disableWebPagePreview: true, replyMarkup: new ReplyKeyboardMarkup(
                new List<KeyboardButton>
                {
                    new("Einverstanden"),
                    new("Ich habe doch kein Interesse mehr")
                })
            {
                OneTimeKeyboard = true
            });

        _logger.LogDebug("Start Keyboard sent.");
    }
}