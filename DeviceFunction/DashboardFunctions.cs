using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace DeviceFunction;

public static class DashboardFunctions
{
    [FunctionName("negotiate")]
    public static SignalRConnectionInfo GetSignalRInfo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
        [SignalRConnectionInfo(HubName = DeviceEntity.SignalRHubName)] SignalRConnectionInfo connectionInfo)
    {
        // AzureSignalRConnectionString environment variable is defined with the serverless SignalR service ConnString.

        return connectionInfo;
    }

    [FunctionName(nameof(Dashboard))]
    public static async Task<HttpResponseMessage> Dashboard([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req)
    {
        //string path = Path.Combine(context.FunctionAppDirectory, "dashboard.html"); // This could be pass in the method: ExecutionContext context
        string dashboardHTML = await File.ReadAllTextAsync("dashboard.html");

        return new HttpResponseMessage
        {
            Content = new StringContent(dashboardHTML, Encoding.UTF8, MediaTypeNames.Text.Html)
        };
    }

    // Using Azure Functions built-in authentication
    // https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-concept-serverless-development-config#using-app-service-authentication
    
    //[FunctionName("negotiate")]
    //public static IActionResult GetSignalRInfoAuthenticated(
    //    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
    //    IBinder binder)
    //{
    //    if (req.Headers.ContainsKey("Authorization") && req.Headers["Authorization"].ToString().StartsWith("Bearer "))
    //    {
    //        string userId = "userIdExctractedFromToken"; // Needs real implementation.

    //        var attribute = new SignalRConnectionInfoAttribute { HubName = "messages", UserId = userId };

    //        var connectionInfo = binder.Bind<SignalRConnectionInfo>(attribute);

    //        return new OkObjectResult(connectionInfo);
    //    }

    //    return new StatusCodeResult(StatusCodes.Status401Unauthorized);
    //}
}
