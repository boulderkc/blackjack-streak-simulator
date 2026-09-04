namespace BlackjackStreakSimulator.Data;

public class BatchHistoryEntry
{
    public int Id { get; set; }
    public DateTime CompletedAtUtc { get; set; }

    // Config used for this batch
    public decimal InitialBankroll { get; set; }
    public decimal BaseBet { get; set; }
    public decimal BankrollGoal { get; set; }
    public int DecksInShoe { get; set; }
    public int MaxStreakCount { get; set; }
    public int SeatCount { get; set; }
    public string BettingMode { get; set; } = "";

    // How many Runs this batch consisted of
    public int RunCount { get; set; }
    public int TimesReachedGoal { get; set; }
    public int TimesBusted { get; set; }

    public double AverageHandsPlayedWhenReachedGoal { get; set; }
    public double AverageHandsPlayedWhenBusted { get; set; }
    public decimal AverageMaxDrawdownWhenReachedGoal { get; set; }
    public decimal WorstMaxDrawdownWhenReachedGoal { get; set; }

    public ICollection<StreakLengthFrequencyEntry> StreakLengthFrequencies { get; set; } = new List<StreakLengthFrequencyEntry>();
    public ICollection<LowestBankrollBucketEntry> LowestBankrollBuckets { get; set; } = new List<LowestBankrollBucketEntry>();
}
