using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BlackjackStreakSimulator.Data;

// Used only by the `dotnet ef` CLI tooling at design time (generating
// migrations) - this class library has no host/DI container of its own
// to pull a connection string from, so this hardcoded LocalDB connection
// is a design-time convenience only, not what the app uses at runtime.
public class BlackjackStreakSimulatorDbContextFactory : IDesignTimeDbContextFactory<BlackjackStreakSimulatorDbContext>
{
    public BlackjackStreakSimulatorDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<BlackjackStreakSimulatorDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=BlackjackStreakSimulator;Trusted_Connection=True;");

        return new BlackjackStreakSimulatorDbContext(optionsBuilder.Options);
    }
}
