namespace BlackjackStreakSimulator.Engine.Tests;

public class SingleRoundRunnerTests
{
    [Fact]
    public void PlayNextRound_UpdatesStateEachCall()
    {
        SingleRoundRunner runner = new SingleRoundRunner(new SimulationConfig());

        runner.PlayNextRound();

        Assert.Equal(1, runner.HandsPlayed);
        Assert.NotNull(runner.LastDealerHand);
        Assert.True(runner.LastDealerHand!.Cards.Count >= 2);
        Assert.False(runner.IsFinished);
        Assert.Null(runner.Result);
    }

    [Fact]
    public void PlayNextRound_DoesNothingOnceFinished()
    {
        SingleRoundRunner runner = new SingleRoundRunner(new SimulationConfig());
        runner.FinishAutomatically();
        int handsAtFinish = runner.HandsPlayed;

        runner.PlayNextRound(); // should be a no-op once IsFinished is true

        Assert.Equal(handsAtFinish, runner.HandsPlayed);
    }

    [Fact]
    public void FinishAutomatically_EndsWithResultSet()
    {
        SingleRoundRunner runner = new SingleRoundRunner(new SimulationConfig());

        runner.FinishAutomatically();

        Assert.True(runner.IsFinished);
        Assert.NotNull(runner.Result);
        Assert.Equal(runner.HandsPlayed, runner.Result!.HandsPlayed);
    }

    [Fact]
    public void FinishAutomatically_AfterStepThroughRounds_ContinuesFromThatPoint()
    {
        SingleRoundRunner runner = new SingleRoundRunner(new SimulationConfig());
        runner.PlayNextRound();
        runner.PlayNextRound();
        int handsBeforeFinish = runner.HandsPlayed;

        runner.FinishAutomatically();

        Assert.True(runner.IsFinished);
        Assert.True(runner.HandsPlayed >= handsBeforeFinish);
    }
}
