namespace BlackjackStreakSimulator.Web.Components.Pages;

using System.Diagnostics;
using BlackjackStreakSimulator.Engine;
using BlackjackStreakSimulator.Web.Services;
using Microsoft.AspNetCore.Components;

public partial class BatchSimulation
{
    [Inject] public SimulationConfigState ConfigState { get; set; }

    [Inject] public IHttpClientFactory HttpClientFactory { get; set; }

    public bool IsRunning { get; set; }
    public BatchSimulationResult? LastResult { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? LastRunDuration { get; set; }

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
            HttpResponseMessage response = await client.PostAsJsonAsync("api/RunAndRecordBatchFunction", ConfigState.Current);

            if (response.IsSuccessStatusCode)
            {
                LastResult = await response.Content.ReadFromJsonAsync<BatchSimulationResult>();
            }
            else
            {
                ErrorMessage = await response.Content.ReadAsStringAsync();
            }
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
