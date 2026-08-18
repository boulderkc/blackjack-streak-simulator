namespace BlackjackStreakSimulator.Engine;

public interface IBettingStrategy
{
    public (int Bet, bool StreakCompleted) GetNextBet(int baseBet, int streakCount);
}
