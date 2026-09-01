namespace BlackjackStreakSimulator.Engine;

// Aggregate across many independent Runs (each a full SimulationResult -
// one seat's playthrough from InitialBankroll to bust or goal), not a
// single Run. FinalBankroll deliberately has no equivalent here - unlike
// streak length or drawdown, it doesn't carry useful information in
// aggregate (a reached-goal run's final bankroll clusters near
// BankrollGoal, a busted run's clusters near 0, by construction of the
// stopping condition).
public class BatchSimulationResult
{
    public int RunCount { get; set; }
    public int TimesReachedGoal { get; set; }
    public int TimesBusted { get; set; }

    // Every run's SimulationResult.StreakLengthFrequency summed together,
    // key-by-key - the real headline stat for comparing Streak vs. Flat
    // across many runs, same reasoning as the per-run version.
    public Dictionary<int, int> StreakLengthFrequency { get; set; } = [];

    public double AverageHandsPlayed { get; set; }
    public decimal AverageMaxDrawdown { get; set; }
    public decimal WorstMaxDrawdown { get; set; }

    // How close to bankruptcy reached-goal runs got along the way, not
    // just that they succeeded - key = bucket floor (0, 10, ... 90) of
    // lowest-bankroll-as-a-percentage-of-initial, value = how many
    // reached-goal runs fell in that bucket. Busted runs aren't included -
    // their lowest bankroll is trivially near zero by definition of how
    // they ended, so it isn't an interesting question for them. See
    // SimulationLoop.GetLowestBankrollBucket.
    public Dictionary<int, int> LowestBankrollBucketFrequency { get; set; } = [];
}
