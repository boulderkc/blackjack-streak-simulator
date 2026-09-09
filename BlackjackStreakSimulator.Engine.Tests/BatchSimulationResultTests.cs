using System.Text.Json;

namespace BlackjackStreakSimulator.Engine.Tests;

public class BatchSimulationResultTests
{
    // Guards against the exact bug this class already hit once: any
    // private-set/get-only property without [JsonInclude] silently comes
    // back as its default value after a JSON round trip instead of
    // erroring - easy to reintroduce the moment a new field gets added
    // without remembering the attribute.
    [Fact]
    public void SurvivesJsonRoundTrip()
    {
        BatchSimulationResult original = new BatchSimulationResult();
        original.AddRun(new SimulationResult
        {
            ReachedGoal = true,
            HandsPlayed = 42,
            MaxDrawdown = 150m,
            LowestBankroll = 700m,
            StreakLengthFrequency = new Dictionary<int, int> { [3] = 1 }
        }, initialBankroll: 1000m);
        original.AddRun(new SimulationResult
        {
            ReachedGoal = false,
            HandsPlayed = 17
        }, initialBankroll: 1000m);

        string json = JsonSerializer.Serialize(original);
        BatchSimulationResult? roundTripped = JsonSerializer.Deserialize<BatchSimulationResult>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(original.RunCount, roundTripped.RunCount);
        Assert.Equal(original.TimesReachedGoal, roundTripped.TimesReachedGoal);
        Assert.Equal(original.TimesBankrupt, roundTripped.TimesBankrupt);
        Assert.Equal(original.WorstMaxDrawdownWhenReachedGoal, roundTripped.WorstMaxDrawdownWhenReachedGoal);
        Assert.Equal(original.AverageHandsPlayedWhenReachedGoal, roundTripped.AverageHandsPlayedWhenReachedGoal);
        Assert.Equal(original.AverageHandsPlayedWhenBankrupt, roundTripped.AverageHandsPlayedWhenBankrupt);
        Assert.Equal(original.AverageMaxDrawdownWhenReachedGoal, roundTripped.AverageMaxDrawdownWhenReachedGoal);
        Assert.Equal(original.StreakLengthFrequency, roundTripped.StreakLengthFrequency);
        Assert.Equal(original.LowestBankrollBucketFrequency, roundTripped.LowestBankrollBucketFrequency);

        // The actual symptom that prompted this test: every one of the
        // above should be a real, non-default value, not just equal to
        // each other by coincidence of both being zero.
        Assert.True(roundTripped.RunCount > 0);
        Assert.True(roundTripped.WorstMaxDrawdownWhenReachedGoal > 0);
    }
}
