namespace BlackjackStreakSimulator.Engine;

public interface IBettingStrategy
{
    public (decimal Bet, bool StreakCompleted) GetNextBet(decimal baseBet, int streakCount);
}
