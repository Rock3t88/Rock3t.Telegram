using Akinator.Api.Net.Model;

namespace Rock3t.Telegram.Lib.Akinator;

public class GuessChangedEventArgs : EventArgs
{
    public AkinatorGuess Guess { get; }
    public bool IsRightGuess { get; set; } = false;

    public GuessChangedEventArgs(AkinatorGuess quess)
    {
        Guess = quess;
    }
}