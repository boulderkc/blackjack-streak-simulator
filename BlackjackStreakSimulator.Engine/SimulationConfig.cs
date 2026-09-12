using System.ComponentModel.DataAnnotations;

namespace BlackjackStreakSimulator.Engine;

public class SimulationConfig : IValidatableObject
{
    // Shared bounds - both the [Range] attributes below and the config
    // page's MudNumericField Min/Max parameters reference these constants
    // directly, so a bound only ever needs to change in one place.
    //
    // Decimal fields are the one exception: C# attribute arguments must be
    // compile-time constants of specific types, and decimal isn't one of
    // them, so [Range] needs its bounds spelled out as strings there (see
    // below) rather than referencing these decimal constants directly. The
    // constants themselves still back the Razor markup's Min, so the
    // numbers only live in two spots for those three fields instead of one -
    // a real, if minor, papercut, not a design choice.
    public const decimal InitialBankrollMin = 1m;
    public const decimal BankrollGoalMin = 1m;
    public const decimal BaseBetMin = 1m;
    public const int MaxStreakCountMin = 2; // 1 is just flat betting with extra steps
    public const int MaxStreakCountMax = 10;
    public const int DecksInShoeMin = 1;
    public const int DecksInShoeMax = 10;
    public const int SeatCountMin = 1;
    public const int SeatCountMax = 5;
    public const int NumberOfRunsInBatchMin = 1;
    public const int NumberOfRunsInBatchMax = 100000;

    [Range(typeof(decimal), "1", "79228162514264337593543950335")] // InitialBankrollMin, decimal.MaxValue
    public decimal InitialBankroll {get; set;}

    [Range(typeof(decimal), "1", "79228162514264337593543950335")] // BaseBetMin, decimal.MaxValue
    public decimal BaseBet {get; set;}

    [Range(DecksInShoeMin, DecksInShoeMax)]
    public int DecksInShoe {get; set;}

    [Range(MaxStreakCountMin, MaxStreakCountMax)]
    public int MaxStreakCount {get; set;}

    [Range(SeatCountMin, SeatCountMax)]
    public int SeatCount {get; set;}

    [Range(typeof(decimal), "1", "79228162514264337593543950335")] // BankrollGoalMin, decimal.MaxValue
    public decimal BankrollGoal {get; set;}

    public BettingMode BettingMode {get; set;}

    [Range(NumberOfRunsInBatchMin, NumberOfRunsInBatchMax)]
    public int NumberOfRunsInBatch {get; set;}

    public SimulationConfig()
    {
        InitialBankroll = 1000m;
        BankrollGoal = 3000m;
        BaseBet = 100m;
        DecksInShoe = 6;
        MaxStreakCount = 3;
        SeatCount = 5;
        BettingMode = BettingMode.Streak;
        NumberOfRunsInBatch = 1000;
    }

    // The single source of truth for cross-field config rules. Both the Web
    // config page (via SimulationConfigState) and the batch Function call
    // Validator.TryValidateObject against this same SimulationConfig - a
    // rule only ever needs to change here, once. Validator.TryValidateObject
    // runs both the [Range] attributes above and this method in the same
    // pass.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (InitialBankroll >= BankrollGoal)
        {
            yield return new ValidationResult(
                "Bankroll goal must be larger than initial bankroll.",
                [nameof(BankrollGoal)]);
        }

        if (BaseBet >= InitialBankroll)
        {
            yield return new ValidationResult(
                "Base bet must be smaller than initial bankroll.",
                [nameof(BaseBet)]);
        }

        if (DecksInShoe < SeatCount)
        {
            yield return new ValidationResult(
                "To avoid reshuffles in the middle of a round, must have at least one deck per player.",
                [nameof(DecksInShoe)]);
        }
    }
}
