namespace BlackjackStreakSimulator.Web.Components.Pages;

using System.Diagnostics;
using BlackjackStreakSimulator.Engine;
using BlackjackStreakSimulator.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

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

    public async Task RunBatch()
    {
        IsRunning = true;
        ErrorMessage = null;
        LastResult = null;
        LastRunDuration = null;

        Stopwatch stopwatch = Stopwatch.StartNew();

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
        catch (Exception ex)
        {
            // Previously uncaught - a network-level failure (DNS, refused
            // connection, TLS, timeout) reaching the Function would escape
            // this method entirely as an unhandled circuit exception rather
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
            LastRunDuration = stopwatch.Elapsed;
            IsRunning = false;
        }
    }
}
