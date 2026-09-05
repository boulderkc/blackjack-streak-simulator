namespace BlackjackStreakSimulator.Web.Components.Pages;

using BlackjackStreakSimulator.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

public partial class BatchHistory
{
    [Inject] public BlackjackStreakSimulatorDbContext DbContext { get; set; }

    public List<BatchHistoryEntry> Histories { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        Histories = await DbContext.BatchHistories
            .Include(b => b.StreakLengthFrequencies)
            .Include(b => b.LowestBankrollBuckets)
            .OrderByDescending(b => b.CompletedAtUtc)
            .ToListAsync();
    }
}
