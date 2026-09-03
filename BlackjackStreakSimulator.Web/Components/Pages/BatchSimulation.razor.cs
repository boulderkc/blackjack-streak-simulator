namespace BlackjackStreakSimulator.Web.Components.Pages;

using BlackjackStreakSimulator.Engine;
using BlackjackStreakSimulator.Web.Services;
using Microsoft.AspNetCore.Components;

public partial class BatchSimulation
{
    [Inject] public SimulationConfigState ConfigState { get; set; }

    [Inject] public IHttpClientFactory HttpClientFactory { get; set; }

    public async Task RunBatch()
    {
        HttpClient client = HttpClientFactory.CreateClient("BatchFunction");
        HttpResponseMessage response = await client.PostAsJsonAsync("api/RunAndRecordBatchFunction", ConfigState.Current);

        if (response.IsSuccessStatusCode)
        {
            BatchSimulationResult? echoed = await response.Content.ReadFromJsonAsync<BatchSimulationResult>();
        }
        else
        {
            string error = await response.Content.ReadAsStringAsync();
            // surface `error` to the user - it'll be your validation messages
            // if the config was invalid, straight from BatchFunction.cs
        }
    }


}
