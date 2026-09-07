using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BlackjackStreakSimulator.Engine;
using BlackjackStreakSimulator.Data;

namespace BlackjackStreakSimulator.Functions;

public class RunAndRecordBatch
{
    private readonly ILogger<RunAndRecordBatch> _logger;
    private readonly BlackjackStreakSimulatorDbContext _dbContext;

    public RunAndRecordBatch(ILogger<RunAndRecordBatch> logger, BlackjackStreakSimulatorDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [Function("RunAndRecordBatchFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("RunAndRecordBatchFunction invoked.");

        SimulationConfig? config;

        try
        {
            config = await JsonSerializer.DeserializeAsync<SimulationConfig>(
                req.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Request body was malformed JSON.");
            return new BadRequestObjectResult("Malformed JSON body.");
        }

        if (config is null)
        {
            _logger.LogWarning("Request body deserialized to null.");
            return new BadRequestObjectResult("Request body is required.");
        }

        _logger.LogInformation("Config deserialized: {RunCount} runs requested.", config.NumberOfRunsInBatch);

        // Same Validator.TryValidateObject call SimulationConfigState uses
        // on the Web side, against the exact same SimulationConfig - the
        // [Range] attributes and IValidatableObject.Validate cross-field
        // rules both run in this one pass, no per-field checks to
        // maintain here or fall out of sync with the Web side.
        List<ValidationResult> validationResults = [];
        bool isValid = Validator.TryValidateObject(config, new ValidationContext(config), validationResults, validateAllProperties: true);

        if (!isValid)
        {
            string errors = string.Join(" ", validationResults.Select(r => r.ErrorMessage));
            _logger.LogWarning("Config failed validation: {Errors}", errors);
            return new BadRequestObjectResult(errors);
        }

        BatchSimulationResult batchResult = new BatchSimulationResult();
        // Begin the simulation
        for (int i = 0; i < config.NumberOfRunsInBatch; i++)
        {
            batchResult.AddRun(SimulationRunner.RunSimulation(config), config.InitialBankroll);
        }

        _logger.LogInformation("Batch of {RunCount} runs complete. Saving to database.", batchResult.RunCount);

        BatchHistoryEntry historyEntry = new BatchHistoryEntry
        {
            CompletedAtUtc = DateTime.UtcNow,
            InitialBankroll = config.InitialBankroll,
            BaseBet = config.BaseBet,
            BankrollGoal = config.BankrollGoal,
            DecksInShoe = config.DecksInShoe,
            MaxStreakCount = config.MaxStreakCount,
            SeatCount = config.SeatCount,
            BettingMode = config.BettingMode.ToString(),
            RunCount = batchResult.RunCount,
            TimesReachedGoal = batchResult.TimesReachedGoal,
            TimesBusted = batchResult.TimesBusted,
            AverageHandsPlayedWhenReachedGoal = batchResult.AverageHandsPlayedWhenReachedGoal,
            AverageHandsPlayedWhenBusted = batchResult.AverageHandsPlayedWhenBusted,
            AverageMaxDrawdownWhenReachedGoal = batchResult.AverageMaxDrawdownWhenReachedGoal,
            WorstMaxDrawdownWhenReachedGoal = batchResult.WorstMaxDrawdownWhenReachedGoal
        };

        foreach ((int length, int count) in batchResult.StreakLengthFrequency)
        {
            historyEntry.StreakLengthFrequencies.Add(new StreakLengthFrequencyEntry
            {
                StreakLength = length,
                Count = count
            });
        }

        foreach ((int bucket, int count) in batchResult.LowestBankrollBucketFrequency)
        {
            historyEntry.LowestBankrollBuckets.Add(new LowestBankrollBucketEntry
            {
                BucketFloor = bucket,
                Count = count
            });
        }

        try
        {
            _dbContext.BatchHistories.Add(historyEntry);
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // The batch itself ran fine - this is specifically the database
            // write failing (LocalDB/Azure SQL unreachable, constraint
            // violation, etc.). Treated as a failure of the whole request
            // anyway, not a partial success returning batchResult without
            // saving it - this endpoint's job is to run AND record, and a
            // result nobody can find again later in history isn't much use.
            _logger.LogError(ex, "Failed to save batch result to the database.");
            return new ObjectResult("The batch ran successfully, but the result could not be saved. Please try again.")
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        _logger.LogInformation("Batch result saved. Returning response.");
        return new OkObjectResult(batchResult);
    }
}
