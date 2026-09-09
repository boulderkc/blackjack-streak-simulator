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
    public void RunSimulation_RealGameplay_RecordsZeroLengthStreaksFromConsecutiveLosses()
    {
        // Unlike SeatTests' coverage of this exact behavior, this goes
        // through the real RoundEngine-driven pipeline (actual dealt
        // cards, dealer play, basic strategy) rather than hand-crafted
        // Hand objects assigned directly to seat.Hands - this is the same
        // call the batch Function makes. A loss with no active streak (the
        // very first hand of a run, or any loss immediately following a
        // prior loss) is one of the most common possible outcomes, so
        // aggregating across enough real runs should reliably produce at
        // least one bucket-0 tally if the pipeline actually behaves the
        // way the isolated Seat-level tests already prove the underlying
        // logic should.
        SimulationConfig config = new SimulationConfig();
        Dictionary<int, int> combined = new Dictionary<int, int>();

        for (int i = 0; i < 20; i++)
        {
            SimulationResult result = SimulationRunner.RunSimulation(config);
            foreach ((int length, int count) in result.StreakLengthFrequency)
            {
                combined[length] = combined.GetValueOrDefault(length) + count;
            }
        }

        Assert.True(combined.ContainsKey(0), "Expected at least one zero-length streak (a loss with no active streak) across 20 real runs.");
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
