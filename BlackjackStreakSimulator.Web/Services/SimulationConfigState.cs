using BlackjackStreakSimulator.Engine;

namespace BlackjackStreakSimulator.Web.Services;

// Holds the simulation config the user has set up on the config page, for
// the rest of the circuit (manual and batch sim pages) to read. Scoped, not
// Singleton - each browser circuit gets its own instance, so one user's
// settings never leak into another user's session. Ephemeral by design,
// same as the rest of manual-session state: a refresh tears down the
// circuit and resets this back to defaults, which is an accepted v1
// tradeoff, not a bug.
//
// `Current` is field-initialized to `new()` so a config always exists, even
// if the user never visits the config page - SimulationConfig's own
// constructor already sets every default value, so "never touched config"
// and "submitted the form without changing anything" are the same case.
public class SimulationConfigState
{
    public SimulationConfig Current { get; set; } = new();
}
