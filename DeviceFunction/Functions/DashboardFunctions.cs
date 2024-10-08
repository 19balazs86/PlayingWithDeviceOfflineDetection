using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace DeviceFunction.Functions;

public static class DashboardFunctions
{
    public const string SignalRHubName = "devicestatus";

    [Function("negotiate")]
    public static string GetSignalRInfo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = SignalRHubName)] string connectionInfo)
    {
        // AzureSignalRConnectionString environment variable is defined with the serverless SignalR service ConnString.

        return connectionInfo;
    }

    [Function(nameof(Dashboard))]
    public static async Task<HttpResponseData> Dashboard([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req)
    {
        // string path = Path.Combine(context.FunctionAppDirectory, "dashboard.html"); // 'ExecutionContext context' could be a parameter

        string dashboardHTML = await File.ReadAllTextAsync("dashboard.html");

        return await req.createStringResponseAsync(dashboardHTML);
    }

    [Function(nameof(SendHeartbeat))]
    [QueueOutput(DeviceFunctions.DeviceQueue)]
    public static string[] SendHeartbeat([HttpTrigger(AuthorizationLevel.Anonymous, Route = "send-heartbeat")] HttpRequestData req)
    {
        string[] deviceIds = Enumerable.Range(0, 10).Select(_ => Random.Shared.Next(1, 21).ToString()).ToArray();

        return deviceIds;
    }

    private static async Task<HttpResponseData> createStringResponseAsync(
        this HttpRequestData request,
        string value,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        HttpResponseData response = request.CreateResponse(statusCode);

        await response.WriteStringAsync(value);

        return response;
    }
}
