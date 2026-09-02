using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using BlackjackStreakSimulator.Engine;

namespace BlackjackStreakSimulator.Functions;

public class RunAndRecordBatch
{
    private readonly ILogger<RunAndRecordBatch> _logger;

    public RunAndRecordBatch(ILogger<RunAndRecordBatch> logger) => _logger = logger;

    [Function("RunAndRecordBatchFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        SimulationConfig? config;

        try
        {
            config = await JsonSerializer.DeserializeAsync<SimulationConfig>(
                req.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult("Malformed JSON body.");
        }

        if (config is null)
        {
            return new BadRequestObjectResult("Request body is required.");
        }

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
            return new BadRequestObjectResult(errors);
        }

        // ... write to SQL here ...

        return new OkObjectResult(config);
    }
}
