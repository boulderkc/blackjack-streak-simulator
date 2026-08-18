namespace BlackjackStreakSimulator.Engine;

public class StreakBettingStrategy : IBettingStrategy
{
    private readonly int maxStreakLength;

    public StreakBettingStrategy(int maxStreakLength)
    {
        this.maxStreakLength = maxStreakLength;
    }

    public (int Bet, bool StreakCompleted) GetNextBet(int baseBet, int streakCount)
    {
        int returnBet = baseBet;
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
