using System.ComponentModel.DataAnnotations;
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

    // The rules themselves live on SimulationConfig (IValidatableObject.Validate),
    // not here - this is just a thin adapter translating
    // Validator.TryValidateObject's results into the bool/string shape the
    // config page's markup wants. The batch Function validates the exact
    // same way, against the exact same model, so a rule only ever needs to
    // change in one place.
    private List<ValidationResult> Validate()
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(Current, new ValidationContext(Current), results, validateAllProperties: true);
        return results;
    }

    public bool HasProblems => Validate().Count > 0;

    public string? GetErrorFor(string propertyName)
        => Validate().FirstOrDefault(r => r.MemberNames.Contains(propertyName))?.ErrorMessage;
}
