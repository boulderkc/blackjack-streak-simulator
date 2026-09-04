namespace BlackjackStreakSimulator.Data;

public class StreakLengthFrequencyEntry
{
    public int Id { get; set; }
    public int BatchHistoryEntryId { get; set; }
    public int StreakLength { get; set; }
    public int Count { get; set; }
}
