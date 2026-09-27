namespace BlackjackStreakSimulator.Data;

public class VisitorSessionEntry
{
    public int Id { get; set; }
    public Guid SessionId { get; set; }
    public DateTime StartedAtUtc { get; set; }
}
