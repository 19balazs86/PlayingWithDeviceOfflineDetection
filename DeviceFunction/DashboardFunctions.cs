using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace DeviceFunction
{
  public static class DashboardFunctions
  {
    [FunctionName("negotiate")]
    public static SignalRConnectionInfo GetSignalRInfo(
      [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
      [SignalRConnectionInfo(HubName = "devicestatus")] SignalRConnectionInfo connectionInfo)
    {
      return connectionInfo;
    }

    [FunctionName(nameof(Dashboard))]
    public static HttpResponseMessage Dashboard(
      [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
      ExecutionContext context)
    {
      string path    = Path.Combine(context.FunctionAppDirectory, "dashboard.html");
      string content = File.ReadAllText(path);

      return new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(content, Encoding.UTF8, MediaTypeNames.Text.Html)
      };
    }

    //[FunctionName("negotiate")]
    //public static IActionResult GetSignalRInfoAuthenticated(
    //  [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
    //  IBinder binder)
    //{
    //  if (req.Headers.ContainsKey("Authorization") && req.Headers["Authorization"].ToString().StartsWith("Bearer "))
    //  {
    //    string userId = "userIdExctractedFromToken"; // Needs real implementation.

    //    var attribute = new SignalRConnectionInfoAttribute { HubName = "messages", UserId = userId };

    //    var connectionInfo = binder.Bind<SignalRConnectionInfo>(attribute);

    //    return new OkObjectResult(connectionInfo);
    //  }

    //  return new StatusCodeResult(StatusCodes.Status401Unauthorized);
    //}
  }
}
