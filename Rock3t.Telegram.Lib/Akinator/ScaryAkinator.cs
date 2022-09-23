using Akinator.Api.Net;
using Akinator.Api.Net.Enumerations;
using Akinator.Api.Net.Model;

namespace Rock3t.Telegram.Lib.Akinator;

public class ScaryAkinator : IDisposable
{
    public event EventHandler<QuestionChangedEventArgs> QuestionChanged;
    public event EventHandler<GuessChangedEventArgs> GuessChanged;

    private AkinatorClient? _client;

    public bool Started { get; private set; }

    //public IReadOnlyDictionary<string, AnswerOptions> AnswerOptions { get; }

    public ScaryAkinator()
    {
    }

    public async Task<AkinatorQuestion> StartAsync()
    {
        Started = true;

        var serverLocator = new AkinatorServerLocator();
        var server = await serverLocator.SearchAsync(Language.German, ServerType.Person);

        _client = new AkinatorClient(server);

        return await _client.StartNewGame();
    }

    public async Task<AkinatorQuestion?> Answer(AnswerOptions answer)
    {
        return await _client?.Answer(answer);
    }

    public bool GuessIsDue => _guessIsDue;

    private bool _guessIsDue;

    public bool GetGuessIsDue()
    {
        return _guessIsDue || (_guessIsDue = _client?.GuessIsDue() ?? false);
    }

    public bool ResetGuess()
    {
        return _guessIsDue = false;
    }

    public async Task<AkinatorGuess[]> GetGuess()
    {
        return await _client.GetGuess();
    }

    public AkinatorQuestion CurrentQuestion => _client.CurrentQuestion;

    public void Stop()
    {
        Started = false;
        ResetGuess();
    }

    //public async Task StartAsync()
    //{
    //    // We will search for a german person server to play on.
    //    var serverLocator = new AkinatorServerLocator();
    //    var server = await serverLocator.SearchAsync(Language.German, ServerType.Person);

    //    bool endGame = false;

    //    using var client = new AkinatorClient(server);
    //    // Start a new game

    //    var question = await client.StartNewGame();

    //    while (!endGame)
    //    {
    //        var questionEventArgs = new QuestionChangedEventArgs(client.CurrentQuestion);

    //        QuestionChanged?.Invoke(this, questionEventArgs);

    //        Console.WriteLine();

    //        AnswerOptions answerOption = questionEventArgs.Answer;

    //        // Answer the previous question with "Yes" and get the next one
    //        var answer = await client.Answer(answerOption);

    //        if (client.GetGuessIsDue())
    //        {
    //            AkinatorGuess[] guess = await client.GetGuess();

    //            foreach (AkinatorGuess item in guess)
    //            {
    //                var guessChangedEventArgs = new GuessChangedEventArgs(item);

    //                GuessChanged?.Invoke(this, guessChangedEventArgs);

    //                if (guessChangedEventArgs.IsRightGuess)
    //                {
    //                    endGame = true;
    //                    break;
    //                }
    //            }
    //        }
    //    }
    //}

    public void Dispose()
    {
        _client.Dispose();
    }
}