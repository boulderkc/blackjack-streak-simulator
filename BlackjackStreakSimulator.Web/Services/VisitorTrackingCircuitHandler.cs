namespace BlackjackStreakSimulator.Web.Services;

using BlackjackStreakSimulator.Data;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;

// Self-owned replacement for the Application Insights Users/Sessions count,
// which never worked reliably for this server-rendered app (see git history
// around the ai_user cookie). One row per Blazor circuit - a circuit opens
// once per browser tab/session, which is a closer match to "a visitor" than
// per-request telemetry. Deliberately no IP address - not needed for a
// simple visitor count and not worth the privacy/complexity tradeoff.
public class VisitorTrackingCircuitHandler(IDbContextFactory<BlackjackStreakSimulatorDbContext> dbContextFactory) : CircuitHandler
{
    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        try
        {
            // A separate, short-lived context from the factory rather than
            // an injected scoped one - same reasoning as MainLayout's
            // database warm-up ping, this shouldn't compete with whatever
            // the rest of the circuit is doing with its own DbContext.
            await using BlackjackStreakSimulatorDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            context.VisitorSessions.Add(new VisitorSessionEntry
            {
                SessionId = Guid.NewGuid(),
                StartedAtUtc = DateTime.UtcNow,
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Best-effort only - a visitor count that occasionally misses a
            // row during a database blip isn't worth failing circuit
            // startup over.
        }

        await base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }
}
