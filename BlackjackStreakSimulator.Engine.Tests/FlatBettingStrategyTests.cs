namespace BlackjackStreakSimulator.Engine.Tests;

public class FlatBettingStrategyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void GetNextBet_AnyStreakCount_ReturnsBaseBetAndNeverCompletes(int streakCount)
    {
        FlatBettingStrategy strategy = new FlatBettingStrategy();

        (decimal bet, bool streakCompleted) = strategy.GetNextBet(baseBet: 10, streakCount);

        Assert.Equal(10m, bet);
        Assert.False(streakCompleted);
    }
}
