namespace BlackjackStreakSimulator.Engine;

public class SimulationResult
{
    public bool ReachedGoal { get; set; }
    public int HandsPlayed { get; set; }
    public decimal FinalBankroll { get; set; }

    // Key = streak length (1 through the configured MaxStreakCount), value =
    // how many times a streak of that length concluded during the run -
    // covers both a loss breaking an active streak and hitting the
    // configured max ("cash out"). See docs/DECISIONS.md - "longest streak"
    // is just the highest populated key, no separate field needed.
    public Dictionary<int, int> StreakLengthFrequency { get; set; } = [];

    // Largest peak-to-trough decline in bankroll observed at any point
    // during the run, not just the final bankroll vs. starting bankroll.
    public decimal MaxDrawdown { get; set; }

    // Lowest the bankroll ever got during the run, including the starting
    // value if it never dropped below that. Batch aggregation buckets this
    // as a percentage of InitialBankroll - see
    // SimulationLoop.GetLowestBankrollBucket.
    public decimal LowestBankroll { get; set; }
}
