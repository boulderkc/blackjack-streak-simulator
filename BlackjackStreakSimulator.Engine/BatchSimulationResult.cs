using System.Text.Json.Serialization;

namespace BlackjackStreakSimulator.Engine;

// Aggregate across many independent Runs (each a full SimulationResult -
// one seat's playthrough from InitialBankroll to bankruptcy or goal), not a
// single Run. FinalBankroll deliberately has no equivalent here - unlike
// streak length or drawdown, it doesn't carry useful information in
// aggregate (a reached-goal run's final bankroll clusters near
// BankrollGoal, a bankrupt run's clusters near 0, by construction of the
// stopping condition).
//
// Every private-set/get-only property below carries [JsonInclude] - this
// type doubles as the wire format the batch Function sends back to Blazor
// (per CLAUDE.md, shared shapes are reused across hosts, not duplicated),
// and System.Text.Json silently skips any property without a public
// setter during deserialization by default. Without [JsonInclude], a
// round trip through JSON quietly resets every one of these back to its
// default (0, empty dictionary) instead of erroring - [JsonInclude] tells
// it to use the non-public setter anyway, while AddRun stays the only
// thing that can mutate this class from regular C# code.
public class BatchSimulationResult
{
    // Plain public setter, unlike everything below it - this is simple
    // metadata set once from outside (by BatchFunction, mirroring the exact
    // value it also writes to BatchHistoryEntry) rather than an
    // AddRun-accumulated stat, so it doesn't need the private-set +
    // [JsonInclude] guard the rest of this class uses.
    public DateTime CompletedAtUtc { get; set; }

    [JsonInclude]
    public int RunCount { get; private set; }

    [JsonInclude]
    public int TimesReachedGoal { get; private set; }

    [JsonInclude]
    public int TimesBankrupt { get; private set; }

    // Every run's SimulationResult.StreakLengthFrequency summed together,
    // key-by-key - the real headline stat for comparing Streak vs. Flat
    // across many runs, same reasoning as the per-run version. Unlike the
    // drawdown/lowest-bankroll stats below, this isn't reached-goal-only -
    // a run's streaks over its lifetime are worth counting regardless of
    // how it ultimately ended.
    [JsonInclude]
    public Dictionary<int, int> StreakLengthFrequency { get; private set; } = [];

    // Split by outcome rather than one blended average across both -
    // a bankrupt run and a reached-goal run likely have systematically
    // different typical lengths, so averaging them together describes
    // neither well (the same reasoning FinalBankroll never got an
    // aggregate field over at all).
    public double AverageHandsPlayedWhenReachedGoal
        => TimesReachedGoal == 0 ? 0 : (double)HandsPlayedSumWhenReachedGoal / TimesReachedGoal;

    public double AverageHandsPlayedWhenBankrupt
        => TimesBankrupt == 0 ? 0 : (double)HandsPlayedSumWhenBankrupt / TimesBankrupt;

    // Reached-goal runs only, same reasoning as LowestBankrollBucketFrequency
    // below - a bankrupt run's max drawdown is trivially close to its entire
    // initial bankroll (that's what going bankrupt means), so including
    // bankrupt runs would just dilute the average with near-identical,
    // uninteresting values. Kept as average+worst rather than bucketed like lowest
    // bankroll - the pair at least shows both "typical" and "worst case"
    // rather than a single number pretending to summarize a possibly
    // bimodal distribution, though full bucketing is a reasonable future
    // upgrade if average+worst ever turns out to hide too much.
    public decimal AverageMaxDrawdownWhenReachedGoal
        => TimesReachedGoal == 0 ? 0m : MaxDrawdownSumWhenReachedGoal / TimesReachedGoal;

    [JsonInclude]
    public decimal WorstMaxDrawdownWhenReachedGoal { get; private set; }

    // How close to bankruptcy reached-goal runs got along the way, not
    // just that they succeeded - key = bucket floor (0, 10, ... 90) of
    // lowest-bankroll-as-a-percentage-of-initial, value = how many
    // reached-goal runs fell in that bucket. Bankrupt runs aren't included -
    // their lowest bankroll is trivially near zero by definition of how
    // they ended, so it isn't an interesting question for them. See
    // SimulationLoop.GetLowestBankrollBucket.
    [JsonInclude]
    public Dictionary<int, int> LowestBankrollBucketFrequency { get; private set; } = [];

    // Backing sums for the computed averages above - promoted from plain
    // private fields to private-set properties (rather than staying
    // fields) specifically so [JsonInclude] has a property to attach to;
    // System.Text.Json only ever considers properties and public fields,
    // never private fields, no matter what attribute you put on them.
    [JsonInclude]
    public int HandsPlayedSumWhenReachedGoal { get; private set; }

    [JsonInclude]
    public int HandsPlayedSumWhenBankrupt { get; private set; }

    [JsonInclude]
    public decimal MaxDrawdownSumWhenReachedGoal { get; private set; }

    // initialBankroll is passed separately rather than read off the result -
    // SimulationResult doesn't carry it (every run in one batch shares the
    // same SimulationConfig, so there's no reason to duplicate it onto
    // every single result just for this one bucketing calculation).
    public void AddRun(SimulationResult result, decimal initialBankroll)
    {
        RunCount++;

        foreach ((int length, int count) in result.StreakLengthFrequency)
        {
            StreakLengthFrequency[length] = StreakLengthFrequency.GetValueOrDefault(length) + count;
        }

        if (result.ReachedGoal)
        {
            TimesReachedGoal++;
            HandsPlayedSumWhenReachedGoal += result.HandsPlayed;
            MaxDrawdownSumWhenReachedGoal += result.MaxDrawdown;
            WorstMaxDrawdownWhenReachedGoal = Math.Max(WorstMaxDrawdownWhenReachedGoal, result.MaxDrawdown);

            int bucket = SimulationLoop.GetLowestBankrollBucket(result.LowestBankroll, initialBankroll);
            LowestBankrollBucketFrequency[bucket] = LowestBankrollBucketFrequency.GetValueOrDefault(bucket) + 1;
        }
        else
        {
            TimesBankrupt++;
            HandsPlayedSumWhenBankrupt += result.HandsPlayed;
        }
    }
}
