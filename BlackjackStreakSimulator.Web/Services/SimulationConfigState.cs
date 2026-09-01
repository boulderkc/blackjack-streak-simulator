using BlackjackStreakSimulator.Engine;

namespace BlackjackStreakSimulator.Web.Services;

// Holds the simulation config the user has set up on the config page, for
// the rest of the circuit (step-through and batch sim pages) to read.
// Scoped, not Singleton - each browser circuit gets its own instance, so
// one user's settings never leak into another user's session. Ephemeral by
// design, same as the rest of step-through session state: a refresh tears
// down the circuit and resets this back to defaults, which is an accepted
// v1 tradeoff, not a bug.
//
// `Current` is field-initialized to `new()` so a config always exists, even
// if the user never visits the config page - SimulationConfig's own
// constructor already sets every default value, so "never touched config"
// and "submitted the form without changing anything" are the same case.
public class SimulationConfigState
{
    public SimulationConfig Current { get; set; } = new();

    // Validation lives here rather than on the config page itself, so the
    // exact same check drives both the inline error caption on the config
    // page AND the "disable the run button" logic on step-through/batch sim -
    // one implementation, can't drift out of sync the way two separate
    // copies could.
    public bool HasBankrollGoalError => Current.InitialBankroll >= Current.BankrollGoal;
    public string BankrollGoalErrorText => "Bankroll goal must be larger than initial bankroll.";

    public bool HasBaseBetError => Current.BaseBet >= Current.InitialBankroll;
    public string BaseBetErrorText => "Base bet must be smaller than initial bankroll.";

    // "One deck per player" - avoids reshuffling mid-round for a realistic
    // table size. Not rigorously derived (see docs/DECISIONS.md discussion)
    // but the shoe self-heals on running dry regardless, so getting this
    // exactly right isn't load-bearing.
    public bool HasDeckCountError => Current.DecksInShoe < Current.SeatCount;
    public string DeckCountErrorText => "To avoid reshuffles in the middle of a round, must have at least one deck per player.";

    public bool HasProblems => HasBankrollGoalError || HasBaseBetError || HasDeckCountError;
}
