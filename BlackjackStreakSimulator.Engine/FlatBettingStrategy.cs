namespace BlackjackStreakSimulator.Engine;

public class FlatBettingStrategy : IBettingStrategy
{
    public (decimal Bet, bool StreakCompleted) GetNextBet(decimal baseBet, int streakCount)
    {
        return (baseBet, false); // never changes for flat betting
    }
}
