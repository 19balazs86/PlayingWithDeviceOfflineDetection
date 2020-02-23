using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace DeviceFunction
{
  public interface IDeviceEntity
  {
    Task MessageReceived();
    Task DeviceTimeout();
  }

  [JsonObject(MemberSerialization.OptIn)]
  public class DeviceEntity : IDeviceEntity
  {
    public const string TimeoutQueue = "timeout-messages";

    [JsonProperty]
    public string Id { get; set; } // Entity.Current.EntityKey

    [JsonProperty]
    public DateTime? LastCommunicationDateTime { get; set; }

    [JsonProperty]
    public string TimeoutMessageId { get; set; }

    [JsonProperty]
    public string TimeoutMessagePopReceipt { get; set; }

    private readonly ILogger _logger;
    private readonly IDelayedQueueHandler _timeoutQueueHandler;
    private readonly IAsyncCollector<SignalRMessage> _signalRMessages;

    public DeviceEntity(
      string id,
      ILogger logger,
      IDelayedQueueHandler timeoutQueueHandler,
      IAsyncCollector<SignalRMessage> signalRMessages)
    {
      Id = id;

      _logger              = logger;
      _timeoutQueueHandler = timeoutQueueHandler;
      _signalRMessages     = signalRMessages;
    }

    [FunctionName(nameof(DeviceEntity))]
    public static async Task DispatchDeviceEntity(
      [EntityTrigger] IDurableEntityContext context,
      [SignalR(HubName = "devicestatus")] IAsyncCollector<SignalRMessage> signalRMessages,
      [Queue(TimeoutQueue)] CloudQueue timeoutQueue,
      ILogger logger)
    {
      //if (!context.HasState)
      //  context.SetState(new DeviceEntity(context.EntityKey, logger, timeoutQueue, signalRMessages));

      IDelayedQueueHandler timeoutQueueHandler = new DelayedQueueHandler(timeoutQueue, TimeSpan.FromSeconds(30));

      await context.DispatchAsync<DeviceEntity>(context.EntityKey, logger, timeoutQueueHandler, signalRMessages);
    }

    public async Task MessageReceived()
    {
      LastCommunicationDateTime = DateTime.UtcNow;

      // Update timeout mesage.
      TimeoutMessagePopReceipt = await _timeoutQueueHandler.UpdateMessage(TimeoutMessageId, TimeoutMessagePopReceipt);

      if (string.IsNullOrWhiteSpace(TimeoutMessagePopReceipt))
      {
        // Add timeout mesage.
        (TimeoutMessageId, TimeoutMessagePopReceipt) = await _timeoutQueueHandler.AddMessage(Id);

        await reportDeviceStatus("online");
      }
    }

    public async Task DeviceTimeout()
    {
      TimeoutMessageId         = null;
      TimeoutMessagePopReceipt = null;

      await reportDeviceStatus("offline");
    }

    private async Task reportDeviceStatus(string status)
    {
      var message = new SignalRMessage
      {
        Target    = "statusChanged",
        Arguments = new[] { new { deviceId = Id, status } }
      };

      try
      {
        await _signalRMessages.AddAsync(message);

        _logger.LogInformation($"Device({Id}) is {status}.");
        _logger.LogMetric(status, 1);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to send device status.");
      }
    }
  }
}
