using Microsoft.EntityFrameworkCore;

namespace BlackjackStreakSimulator.Data;

public class BlackjackStreakSimulatorDbContext : DbContext
{
    public BlackjackStreakSimulatorDbContext(DbContextOptions<BlackjackStreakSimulatorDbContext> options)
        : base(options)
    {
    }

    public DbSet<BatchHistoryEntry> BatchHistories => Set<BatchHistoryEntry>();

    // Without this, EF names a table after its DbSet property when one
    // exists (BatchHistories, pluralized) but falls back to the raw class
    // name for entities only reachable via navigation (singular) - forcing
    // all three to the singular class name here instead, for consistency.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BatchHistoryEntry>().ToTable(nameof(BatchHistoryEntry));
        modelBuilder.Entity<StreakLengthFrequencyEntry>().ToTable(nameof(StreakLengthFrequencyEntry));
        modelBuilder.Entity<LowestBankrollBucketEntry>().ToTable(nameof(LowestBankrollBucketEntry));
    }
}
