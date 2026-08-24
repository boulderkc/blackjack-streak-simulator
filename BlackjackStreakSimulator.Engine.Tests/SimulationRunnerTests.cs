namespace BlackjackStreakSimulator.Engine.Tests;

public class SimulationRunnerTests
{
    [Fact]
    public void RunSimulation_AlwaysTerminatesWithAConsistentResult()
    {
        SimulationConfig config = new SimulationConfig();

        for (int i = 0; i < 5; i++)
        {
            SimulationResult result = SimulationRunner.RunSimulation(config);

            Assert.True(result.HandsPlayed > 0);
            Assert.Equal(result.FinalBankroll >= config.BankrollGoal, result.ReachedGoal);
        }
    }

    [Fact]
    public void RunSimulation_FlatMode_BustOverdrawIsBoundedByOneBaseBet()
    {
        // Splits/doubles are only ever offered while affordable, so they can
        // never push the total staked in a round past what was already
        // available - the only way to go negative at all is the round's own
        // base bet exceeding what little bankroll remained. In flat mode
        // that bet is always exactly BaseBet, so the overdraw is bounded by
        // exactly one BaseBet, not some multiple of it.
        SimulationConfig config = new SimulationConfig { BettingMode = BettingMode.Flat, BaseBet = 10m };

        SimulationResult result = SimulationRunner.RunSimulation(config);

        if (!result.ReachedGoal)
        {
            Assert.True(result.FinalBankroll > -config.BaseBet);
        }
    }
}
