namespace BlackjackStreakSimulator.Engine;

public class FlatBettingStrategy : IBettingStrategy
{
    public (int Bet, bool StreakCompleted) GetNextBet(int baseBet, int streakCount)
    {
        return (baseBet, false); // never changes for flat betting
    }
}
