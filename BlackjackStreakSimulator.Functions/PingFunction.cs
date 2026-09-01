using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlackjackStreakSimulator.Functions;

// Bogus placeholder trigger - just proves the Functions host, routing, and
// local debugging pipeline actually work end to end. No real batch
// simulation logic here yet; that gets built deliberately, together.
public class PingFunction
{
    private readonly ILogger<PingFunction> _logger;

    public PingFunction(ILogger<PingFunction> logger)
    {
        _logger = logger;
    }

    [Function("PingFunction")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
