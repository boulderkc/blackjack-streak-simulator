namespace BlackjackStreakSimulator.Web.Components.Pages;

using System.Diagnostics;
using BlackjackStreakSimulator.Engine;
using BlackjackStreakSimulator.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

public partial class BatchSimulation
{
    [Inject] public SimulationConfigState ConfigState { get; set; }

    [Inject] public IHttpClientFactory HttpClientFactory { get; set; }

    [Inject] public ILogger<BatchSimulation> Logger { get; set; }

    [Inject] public IJSRuntime JsRuntime { get; set; }

    public bool IsRunning { get; set; }
    public BatchSimulationResult? LastResult { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? LastRunDuration { get; set; }
    public TimeZoneInfo? ViewerTimeZone { get; set; }
    public int LastAttemptedRunCount { get; set; }

    private Stopwatch? _liveStopwatch;

    // Live-ticking display while a batch is in flight - separate from
    // LastRunDuration, which is only set once the call finishes. Read by
    // the .razor file every ~100ms while IsRunning is true.
    public TimeSpan LiveElapsed => _liveStopwatch?.Elapsed ?? TimeSpan.Zero;

    // Color creeps toward alarming as the live clock approaches the
    // 100-second HttpClient timeout - a visual cue for the "watch it for
    // yourself" framing on the page.
    public Color StopwatchColor => LiveElapsed.TotalSeconds switch
    {
        < 60 => Color.Default,
        < 90 => Color.Warning,
        _ => Color.Error,
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            string timeZoneId = await JsRuntime.InvokeAsync<string>("getBrowserTimeZone");
            ViewerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            StateHasChanged();
        }
    }

    // Same viewer-local-time approach as BatchHistory.razor.cs - falls back
    // to plain UTC until ViewerTimeZone is known (the brief window before
    // the JS interop call above has come back).
    public DateTime GetDisplayTime(DateTime utc)
        => ViewerTimeZone is null ? utc : TimeZoneInfo.ConvertTimeFromUtc(utc, ViewerTimeZone);

    // Ticks StateHasChanged roughly 10x/second so the live stopwatch in the
    // .razor file actually appears to move while the batch call is
    // in-flight - the awaited PostAsJsonAsync below doesn't yield control
    // back to Blazor's render loop on its own. Cancelled (not awaited to
    // natural completion) the moment the batch call finishes.
    private async Task RunLiveClockAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(100));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected every time - this is how the clock loop always ends.
        }
    }

    public async Task RunBatch()
    {
        IsRunning = true;
        ConfigState.IsBatchRunning = true;
        ErrorMessage = null;
        LastResult = null;
        LastRunDuration = null;
        LastAttemptedRunCount = ConfigState.Current.NumberOfRunsInBatch;

        _liveStopwatch = Stopwatch.StartNew();
        using CancellationTokenSource clockCts = new();
        Task clockTask = RunLiveClockAsync(clockCts.Token);

        try
        {
            HttpClient client = HttpClientFactory.CreateClient("BatchFunction");
            Logger.LogInformation("Calling batch Function at {BaseAddress}", client.BaseAddress);

            HttpResponseMessage response = await client.PostAsJsonAsync("api/RunAndRecordBatchFunction", ConfigState.Current);
            Logger.LogInformation("Batch Function responded with status {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                LastResult = await response.Content.ReadFromJsonAsync<BatchSimulationResult>();
            }
            else
            {
                ErrorMessage = await response.Content.ReadAsStringAsync();
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            // This is specifically HttpClient's own Timeout expiring (its
            // default is 100 seconds, unconfigured on the "BatchFunction"
            // client) - distinct from a caller-requested cancellation,
            // which wouldn't carry a TimeoutException as its InnerException.
            Logger.LogWarning(ex, "Batch Function call timed out after {Elapsed}.", _liveStopwatch.Elapsed);
            ErrorMessage = $"{LastAttemptedRunCount:N0} runs in a batch failed. Try again if you want, " +
                "but you'll have better luck if you change to 1,000-3,000 runs in Simulation Configuration.";
        }
        catch (Exception ex)
        {
            // Previously uncaught - a network-level failure (DNS, refused
            // connection, TLS) reaching the Function would escape this
            // method entirely as an unhandled circuit exception rather
            // than surfacing here, leaving no ErrorMessage and no log trail
            // pointing at what actually failed.
            Logger.LogError(ex, "Failed to reach the batch Function.");
            ErrorMessage = $"Could not reach the batch simulation service: {ex.Message}";
        }
        finally
        {
            // Always runs, even if something above throws (a network
            // failure, a bad deserialize, whatever) - without this,
            // IsRunning could get stuck true forever and the button would
            // stay disabled indefinitely with nothing to reset it.
            clockCts.Cancel();
            await clockTask;
            LastRunDuration = _liveStopwatch.Elapsed;
            IsRunning = false;
            ConfigState.IsBatchRunning = false;
        }
    }
}
