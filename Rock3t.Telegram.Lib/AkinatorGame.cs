using Akinator.Api.Net.Enumerations;
using Microsoft.Extensions.Logging;
using Rock3t.Telegram.Lib.Akinator;
using Telegram.Bot.Types;

namespace Rock3t.Telegram.Lib;

public class AkinatorGame : IGame<ScaryAkinator>
{
    private readonly ILogger<AkinatorGame> _logger;
    public Dictionary<AnswerOptions, string[]> PossibleAnswers { get; }

    public event EventHandler<IGame>? GameExited;
    public TelegramBot Bot { get; }
    public Message? LastMessage { get; set; }
    public Message? LastAnswer { get; set; }
    public bool Completed { get; private set; }
    public string Name => "Scary Akinator";

    public Guid Id { get; } = Guid.NewGuid();

    public ScaryAkinator Model { get; } = new();

    public User Player { get; set; }

    private AkinatorGame(ILogger<AkinatorGame> logger)
    {
        _logger = logger;
    }

    public AkinatorGame(TelegramBot bot)
    {
        PossibleAnswers = new Dictionary<AnswerOptions, string[]>();

        PossibleAnswers.Add(AnswerOptions.DontKnow, new[]
        {
            "ka",
            "keine ahnung",
            "kp"
        });
        PossibleAnswers.Add(AnswerOptions.Probably, new[]
        {
            "vielleicht",
            "vielleicht ja",
            "wahrscheinlich ja",
            "wahrscheinlich ja",
            "möglicherweise ja",
            "möglicherweise ja",
            "evtl ja",
            "eher ja"
        });
        PossibleAnswers.Add(AnswerOptions.ProbablyNot, new[]
        {
            "vielleicht nein",
            "vielleicht nicht",
            "wahrscheinlich nein",
            "wahrscheinlich nicht",
            "möglicherweise nein",
            "möglicherweise nicht",
            "eher nicht",
            "eher nein",
            "evtl nein",
            "evtl nicht"
        });
        PossibleAnswers.Add(AnswerOptions.Yes, new[]
        {
            "ja",
            "yes",
            "jup"
        });
        PossibleAnswers.Add(AnswerOptions.No, new[]
        {
            "nein",
            "nicht",
            "no",
            "nope"
        });

        Bot = bot;
    }

    private AnswerOptions GetAnswerOption(string message)
    {
        var option =
            PossibleAnswers.FirstOrDefault(
                msgOption => msgOption.Value.Any(message.Contains)).Key;

        return option;
    }

    public async Task DoUpdates(Update update)
    {
        if (update.Message?.From?.Id != Player.Id) return;

        LastAnswer = update.Message;

        var answerText = update.Message.Text.ToLower();

        var answerOption = GetAnswerOption(answerText);

        Console.WriteLine($"Received answer: {answerOption}");
        //bool yes = update.Message.Text.ToLower().Contains("ja");
        //bool no = update.Message.Text.ToLower().Contains("nein");

        if (Model.GuessIsDue && answerOption != AnswerOptions.Unknown)
        {
            if (answerOption.Equals(AnswerOptions.Yes))
            {
                Completed = true;
                var message = Model.GetGuess().Result.FirstOrDefault()?.PhotoPath?.ToString() ?? "YEAAHH!!!";
                await Bot.SendMessage(update.Message.Chat.Id, message);
                return;
            }
            else if (answerOption.Equals(AnswerOptions.No))
            {
                Model.ResetGuess();
                var message = Model.CurrentQuestion.Text;
                await Bot.SendMessage(update.Message.Chat.Id, message);

                return;
            }
            else
            {
            }
        }

        var messageCallback = "";

        if (Model.GetGuessIsDue())
        {
            messageCallback = $"Ist es {Model.GetGuess().Result.FirstOrDefault()?.Name} ?";
            await Bot.SendMessage(update.Message.Chat.Id, messageCallback);
            return;
        }

        if (answerOption.Equals(AnswerOptions.Unknown))
            return;

        var newQuestion = await Model.Answer(answerOption);
        messageCallback = newQuestion?.Text;
        await Bot.SendMessage(update.Message.Chat.Id, messageCallback);

        //if (answerOption.Equals(AnswerOptions.Yes))
        //{
        //    var newQuestion = await Model.Answer(AnswerOptions.Yes);
        //    var message = newQuestion?.Text;
        //    await Bot.SendMessage(update.Message.Chat.Id, message);
        //}
        //else if (answerOption.Equals(AnswerOptions.No))
        //{
        //    var newQuestion = await Model.Answer(AnswerOptions.No);
        //    var message = newQuestion?.Text;
        //    await Bot.SendMessage(update.Message.Chat.Id, message);
        //}
        //else if (answerOption.Equals(AnswerOptions.Probably))
        //{
        //    var newQuestion = await Model.Answer(AnswerOptions.Probably);
        //    var message = newQuestion?.Text;
        //    await Bot.SendMessage(update.Message.Chat.Id, message);
        //}
        //else if (answerText.Contains("wahrscheinlich nein"))
        //{
        //    var newQuestion = await Model.Answer(AnswerOptions.ProbablyNot);
        //    var message = newQuestion?.Text;
        //    await Bot.SendMessage(update.Message.Chat.Id, message);
        //}
        //else if (answerText.Contains("keine ahnung"))
        //{
        //    var newQuestion = await Model.Answer(AnswerOptions.DontKnow);
        //    var message = newQuestion?.Text;
        //    await Bot.SendMessage(update.Message.Chat.Id, message);
        //}
        //else
        //{
        //    var newQuestion = await Model.Answer(AnswerOptions.Unknown);
        //    var message = newQuestion?.Text;
        //    await Bot.SendMessage(update.Message.Chat.Id, message);
        //}
    }

    public async Task StartAsync(Update update)
    {
        var result = await Task.FromResult(await Model.StartAsync());
        await Bot.SendMessage(Bot.GetChatId(update), result.Text);
    }
}