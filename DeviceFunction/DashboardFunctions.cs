using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DeviceFunction;

public class DashboardFunctions
{
    private readonly ILogger<DashboardFunctions> _logger;

    public DashboardFunctions(ILogger<DashboardFunctions> logger)
    {
        _logger = logger;
    }

    [Function("Function1")]
    public string Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        return "Welcome to Azure Functions!";
    }
}
