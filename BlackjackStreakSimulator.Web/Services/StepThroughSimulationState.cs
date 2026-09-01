using BlackjackStreakSimulator.Engine;

namespace BlackjackStreakSimulator.Web.Services;

// Holds the in-progress step-through session, if any. Scoped, same
// reasoning as SimulationConfigState: one instance per browser circuit, so
// it survives navigating between pages (a plain field on the page component
// does not - Blazor tears down and rebuilds the component on navigation,
// only a Scoped service outlives that). Still ephemeral by design: a
// browser refresh tears down the whole circuit and this resets to null,
// same accepted v1 tradeoff documented for step-through session state
// generally.
public class StepThroughSimulationState
{
    public SingleRoundRunner? CurrentRun { get; set; }

    // True while a run exists and hasn't finished (bust/goal reached) -
    // the config page uses this to lock its controls so the tracked
    // seat's settings can't change out from under a run already in
    // progress.
    public bool IsRunActive => CurrentRun is not null && !CurrentRun.IsFinished;
}
