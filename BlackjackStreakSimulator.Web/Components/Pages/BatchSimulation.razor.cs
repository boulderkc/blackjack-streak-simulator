namespace BlackjackStreakSimulator.Web.Components.Pages;

using BlackjackStreakSimulator.Engine;
using BlackjackStreakSimulator.Web.Services;
using Microsoft.AspNetCore.Components;

public partial class BatchSimulation
{
    [Inject] public SimulationConfigState ConfigState { get; set; }

    public void RunSim()
    {
    }

}
