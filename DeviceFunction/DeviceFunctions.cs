using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace DeviceOfflineDetectionFunction
{
  public static class DeviceFunctions
  {
    [FunctionName(nameof(HandleMessages))]
    public static void HandleMessages(
      [QueueTrigger("device-messages")] string deviceNumber,
      ILogger log)
    {
      log.LogInformation($"Queue trigger function processed: {deviceNumber}");
    }
  }
}
