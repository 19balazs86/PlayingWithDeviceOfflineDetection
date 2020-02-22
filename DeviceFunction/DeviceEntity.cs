using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
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
    public const string TimeOutQueue = "timeout-messages";

    private readonly TimeSpan _offlineAfter = TimeSpan.FromSeconds(30);

    [JsonProperty]
    public string Id { get; set; }

    [JsonProperty]
    public DateTime? LastCommunicationDateTime { get; set; }

    [JsonProperty]
    public string TimeoutQueueMessageId { get; set; }

    [JsonProperty]
    public string TimeoutQueueMessagePopReceipt { get; set; }

    private readonly ILogger _logger;
    private readonly CloudQueue _timeoutQueue;
    private readonly IAsyncCollector<SignalRMessage> _signalRMessages;

    public DeviceEntity(
      string id,
      ILogger logger,
      CloudQueue timeoutQueue,
      IAsyncCollector<SignalRMessage> signalRMessages)
    {
      Id = id;

      _logger          = logger;
      _timeoutQueue    = timeoutQueue;
      _signalRMessages = signalRMessages;
    }

    [FunctionName(nameof(DeviceEntity))]
    public static async Task DispatchDeviceEntity(
      [EntityTrigger] IDurableEntityContext context,
      [SignalR(HubName = "devicestatus")] IAsyncCollector<SignalRMessage> signalRMessages,
      [Queue(TimeOutQueue)] CloudQueue timeoutQueue,
      ILogger logger)
    {
      //if (!context.HasState)
      //  context.SetState(new DeviceEntity(context.EntityKey, logger, timeoutQueue, signalRMessages));

      await context.DispatchAsync<DeviceEntity>(context.EntityKey, logger, timeoutQueue, signalRMessages);
    }

    public async Task MessageReceived()
    {
      LastCommunicationDateTime = DateTime.UtcNow;

      if (await updateTimeoutMessage()) return;

      await addTimeoutMessage();

      await reportDeviceStatus("online");
    }

    public async Task DeviceTimeout()
    {
      TimeoutQueueMessageId         = null;
      TimeoutQueueMessagePopReceipt = null;

      await reportDeviceStatus("offline");
    }

    private async Task addTimeoutMessage()
    {
      var message = new CloudQueueMessage(Id);

      await _timeoutQueue.AddMessageAsync(message, null, _offlineAfter, null, null);

      TimeoutQueueMessageId         = message.Id;
      TimeoutQueueMessagePopReceipt = message.PopReceipt;
    }

    private async Task<bool> updateTimeoutMessage()
    {
      if (TimeoutQueueMessageId is object)
      {
        try
        {
          var message = new CloudQueueMessage(TimeoutQueueMessageId, TimeoutQueueMessagePopReceipt);

          await _timeoutQueue.UpdateMessageAsync(message, _offlineAfter, MessageUpdateFields.Visibility);

          TimeoutQueueMessagePopReceipt = message.PopReceipt;

          return true;
        }
        catch (StorageException)
        {
          // There is a short window... There was a message but not any more.
          // await addTimeoutMessage();
        }
      }

      return false;
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
