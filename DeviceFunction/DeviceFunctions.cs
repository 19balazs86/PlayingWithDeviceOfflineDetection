using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace DeviceFunction
{
  public static class DeviceFunctions
  {
    [FunctionName(nameof(HandleMessages))]
    public static async Task HandleMessages(
      [QueueTrigger("device-messages")] string deviceId,
      [DurableClient] IDurableEntityClient entityClient,
      ILogger log)
    {
      log.LogInformation($"{nameof(HandleMessages)} function processing: {deviceId}");

      await entityClient.SignalEntityAsync<IDeviceEntity>(getEntityId(deviceId), device => device.MessageReceived());
    }

    [FunctionName(nameof(HandleOfflineMessages))]
    public static async Task HandleOfflineMessages(
      [DurableClient] IDurableEntityClient entityClient,
      [QueueTrigger(DeviceEntity.TimeoutQueue)] string deviceId,
      ILogger log)
    {
      log.LogInformation($"{nameof(HandleOfflineMessages)} function processing: {deviceId}");

      await entityClient.SignalEntityAsync<IDeviceEntity>(getEntityId(deviceId), device => device.DeviceTimeout());
    }

    private static EntityId getEntityId(string deviceId)
      => new EntityId(nameof(DeviceEntity), deviceId);
  }
}
