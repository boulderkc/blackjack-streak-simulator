using System.ComponentModel.DataAnnotations;

namespace BlackjackStreakSimulator.Engine.Tests;

public class SimulationConfigTests
{
    // Guards against exactly the bug this caught once already: a new field
    // added without a default value fails its own [Range] check on every
    // config out of the box - and since Validator.TryValidateObject only
    // runs IValidatableObject.Validate() when every property-level
    // attribute already passes, one forgotten default silently blocks
    // every cross-field rule from ever running, for every config, not just
    // ones that actually violate it.
    [Fact]
    public void DefaultConfig_PassesValidation()
    {
        SimulationConfig config = new SimulationConfig();

        List<ValidationResult> results = [];
        bool isValid = Validator.TryValidateObject(config, new ValidationContext(config), results, validateAllProperties: true);

        Assert.True(isValid, string.Join("; ", results.Select(r => r.ErrorMessage)));
    }

    [Fact]
    public void Validate_BankrollGoalNotLargerThanInitialBankroll_ReturnsErrorForBankrollGoal()
    {
        SimulationConfig config = new SimulationConfig
        {
            InitialBankroll = 6000m,
            BankrollGoal = 5000m
        };

        List<ValidationResult> results = [];
        Validator.TryValidateObject(config, new ValidationContext(config), results, validateAllProperties: true);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SimulationConfig.BankrollGoal)));
    }

    [Fact]
    public void Validate_BaseBetNotSmallerThanInitialBankroll_ReturnsErrorForBaseBet()
    {
        SimulationConfig config = new SimulationConfig
        {
            InitialBankroll = 100m,
            BaseBet = 100m
        };

        List<ValidationResult> results = [];
        Validator.TryValidateObject(config, new ValidationContext(config), results, validateAllProperties: true);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SimulationConfig.BaseBet)));
    }

    [Fact]
    public void Validate_FewerDecksThanSeats_ReturnsErrorForDecksInShoe()
    {
        SimulationConfig config = new SimulationConfig
        {
            DecksInShoe = 2,
            SeatCount = 3
        };

        List<ValidationResult> results = [];
        Validator.TryValidateObject(config, new ValidationContext(config), results, validateAllProperties: true);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SimulationConfig.DecksInShoe)));
    }
}
