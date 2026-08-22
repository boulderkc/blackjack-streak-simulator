namespace BlackjackStreakSimulator.Engine.Tests;

public class StreakBettingStrategyTests
{
    [Fact]
    public void GetNextBet_StreakZero_ReturnsBaseBet()
    {
        StreakBettingStrategy strategy = new StreakBettingStrategy(maxStreakLength: 4);

        (decimal bet, bool streakCompleted) = strategy.GetNextBet(baseBet: 10, streakCount: 0);

        Assert.Equal(10m, bet);
        Assert.False(streakCompleted);
    }

    [Theory]
    [InlineData(1, 20)]  // one win so far: base x2
    [InlineData(2, 40)]  // two wins: base x4
    [InlineData(3, 80)]  // three wins: base x8 - one below the configured max
    public void GetNextBet_BelowMaxStreak_DoublesPerWinAndDoesNotComplete(int streakCount, decimal expectedBet)
    {
        StreakBettingStrategy strategy = new StreakBettingStrategy(maxStreakLength: 4);

        (decimal bet, bool streakCompleted) = strategy.GetNextBet(baseBet: 10, streakCount);

        Assert.Equal(expectedBet, bet);
        Assert.False(streakCompleted);
    }

    [Fact]
    public void GetNextBet_ReachingMaxStreak_ResetsToBaseBetAndCompletes()
    {
        // The classic Paroli "cash out" - completing the configured max streak
        // length resets to base bet, same as a loss would, just not caused by one.
        StreakBettingStrategy strategy = new StreakBettingStrategy(maxStreakLength: 4);

        (decimal bet, bool streakCompleted) = strategy.GetNextBet(baseBet: 10, streakCount: 4);

        Assert.Equal(10m, bet);
        Assert.True(streakCompleted);
    }

    [Fact]
    public void GetNextBet_PastMaxStreak_StillResetsAndCompletes()
    {
        // Shouldn't normally happen (Seat resets the streak once completed),
        // but the >= comparison should stay safe if a stray value slips through.
        StreakBettingStrategy strategy = new StreakBettingStrategy(maxStreakLength: 4);

        (decimal bet, bool streakCompleted) = strategy.GetNextBet(baseBet: 10, streakCount: 5);

        Assert.Equal(10m, bet);
        Assert.True(streakCompleted);
    }
}
