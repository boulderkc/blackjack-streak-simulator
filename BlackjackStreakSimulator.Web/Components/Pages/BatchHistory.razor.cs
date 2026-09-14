namespace BlackjackStreakSimulator.Web.Components.Pages;

using BlackjackStreakSimulator.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

public partial class BatchHistory
{
    [Inject] public BlackjackStreakSimulatorDbContext DbContext { get; set; }

    [Inject] public IJSRuntime JsRuntime { get; set; }

    public TimeZoneInfo? ViewerTimeZone { get; set; }

    public List<BatchHistoryEntry> Histories { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        Histories = await DbContext.BatchHistories
            .Include(b => b.StreakLengthFrequencies)
            .Include(b => b.LowestBankrollBuckets)
            .OrderByDescending(b => b.CompletedAtUtc)
            .ToListAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            string timeZoneId = await JsRuntime.InvokeAsync<string>("getBrowserTimeZone");
            ViewerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            StateHasChanged();
        }
    }

    // Falls back to plain UTC until ViewerTimeZone is known - covers the
    // brief window on first render before the JS interop call in
    // OnAfterRenderAsync has come back yet.
    public DateTime GetDisplayTime(DateTime utc)
        => ViewerTimeZone is null ? utc : TimeZoneInfo.ConvertTimeFromUtc(utc, ViewerTimeZone);

    // A raw count alone is misleading across batches of very different
    // sizes - 284 out of 300 runs and 284 out of 10,000 runs tell very
    // different stories, so Reached Goal/Bankrupt show both together.
    public static string GetCountAndPercent(int count, int total)
        => total == 0 ? count.ToString() : $"{count} / {count / (double)total:P1}";
}
