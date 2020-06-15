using System;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DeviceFunction
{
  public interface IDeviceEntity
  {
    Task MessageReceived();
    Task DeviceTimeout();
    Task DeleteDevice();
  }

  [JsonObject(MemberSerialization.OptIn)]
  public class DeviceEntity : IDeviceEntity
  {
    public const string TimeoutQueue = "timeout-messages";
    public const string DeleteQueue  = "delete-devices";

    [JsonProperty]
    public string Id { get; set; } // Entity.Current.EntityKey

    [JsonProperty]
    public string TimeoutMessageId { get; set; }

    [JsonProperty]
    public string TimeoutMessagePopReceipt { get; set; }

    [JsonProperty]
    public string DeleteMessageId { get; set; }

    [JsonProperty]
    public string DeleteMessagePopReceipt { get; set; }

    private readonly ILogger _logger;
    private readonly IAsyncCollector<SignalRMessage> _signalRMessages;
    private readonly IDelayedQueueHandler _timeoutQueueHandler;
    private readonly IDelayedQueueHandler _deleteQueueHandler;

    public DeviceEntity(
      string id,
      ILogger logger,
      IAsyncCollector<SignalRMessage> signalRMessages,
      IDelayedQueueHandler timeoutQueueHandler,
      IDelayedQueueHandler deleteQueueHandler)
    {
      Id = id;

      _logger              = logger;
      _signalRMessages     = signalRMessages;
      _timeoutQueueHandler = timeoutQueueHandler;
      _deleteQueueHandler  = deleteQueueHandler;
    }

    [FunctionName(nameof(DeviceEntity))]
    public static async Task DispatchDeviceEntity(
      [EntityTrigger] IDurableEntityContext context,
      [SignalR(HubName = "devicestatus")] IAsyncCollector<SignalRMessage> signalRMessages,
      [Queue(TimeoutQueue)] CloudQueue timeoutQueue,
      [Queue(DeleteQueue)] CloudQueue deleteQueue,
      ILogger logger)
    {
      //if (!context.HasState)
      //  context.SetState(new DeviceEntity(context.EntityKey, logger, timeoutQueue, signalRMessages));

      var timeoutQueueHandler = new DelayedQueueHandler(timeoutQueue, TimeSpan.FromSeconds(30));
      var deleteQueueHandler  = new DelayedQueueHandler(deleteQueue, TimeSpan.FromMinutes(1));

      await context.DispatchAsync<DeviceEntity>(
        context.EntityKey,
        logger,
        signalRMessages,
        timeoutQueueHandler,
        deleteQueueHandler);
    }

    public async Task MessageReceived()
    {
      // Update timeout mesage.
      TimeoutMessagePopReceipt = await _timeoutQueueHandler.UpdateMessage(TimeoutMessageId, TimeoutMessagePopReceipt);

      if (TimeoutMessagePopReceipt is null)
      {
        // Add timeout mesage.
        (TimeoutMessageId, TimeoutMessagePopReceipt) = await _timeoutQueueHandler.AddMessage(Id);

        await reportDeviceStatus("online");
      }

      // Handle message to delete the device entity.
      await _deleteQueueHandler.DeleteMessage(DeleteMessageId, DeleteMessagePopReceipt);

      DeleteMessageId = DeleteMessagePopReceipt = null;
    }

    public async Task DeviceTimeout()
    {
      TimeoutMessageId = TimeoutMessagePopReceipt = null;

      await reportDeviceStatus("offline");

      // Update delete mesage.
      DeleteMessagePopReceipt = await _deleteQueueHandler.UpdateMessage(DeleteMessageId, DeleteMessagePopReceipt);

      if (DeleteMessagePopReceipt is null) // Add delete mesage.
        (DeleteMessageId, DeleteMessagePopReceipt) = await _deleteQueueHandler.AddMessage(Id);
    }

    public Task DeleteDevice()
    {
      Entity.Current.DeleteState();

      _logger.LogInformation($"Device({Id}) is deleted.");

      return Task.CompletedTask;
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
