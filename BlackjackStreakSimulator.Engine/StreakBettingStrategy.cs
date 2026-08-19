namespace BlackjackStreakSimulator.Engine;

public class StreakBettingStrategy : IBettingStrategy
{
    private readonly int maxStreakLength;

    public StreakBettingStrategy(int maxStreakLength)
    {
        this.maxStreakLength = maxStreakLength;
    }

    public (decimal Bet, bool StreakCompleted) GetNextBet(decimal baseBet, int streakCount)
    {
        decimal returnBet = baseBet;
        bool streakComplete = false;
        if (streakCount >= maxStreakLength)
        {
            returnBet = baseBet;
            streakComplete = true;
        }
        else if (streakCount > 0)
        {
            for (int i = 0; i < streakCount; i++)
            {
                returnBet *= 2;
            }
        }
        return (returnBet, streakComplete);
    }
}
