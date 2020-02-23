using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace DeviceFunction
{
  public interface IDelayedQueueHandler
  {
    Task<(string delayedMessageId, string popReceipt)> AddMessage(string content);
    Task<string> UpdateMessage(string delayedMessageId, string popReceipt);
    Task DeleteMessage(string delayedMessageId, string popReceipt);
  }

  public class DelayedQueueHandler : IDelayedQueueHandler
  {
    private readonly CloudQueue _timeoutQueue;
    private readonly TimeSpan _delayedTimeSpan;

    public DelayedQueueHandler(CloudQueue timeoutQueue, TimeSpan delayedTimeSpan)
    {
      _timeoutQueue = timeoutQueue;
      _delayedTimeSpan = delayedTimeSpan;
    }

    public async Task<(string delayedMessageId, string popReceipt)> AddMessage(string content)
    {
      var message = new CloudQueueMessage(content);

      await _timeoutQueue.AddMessageAsync(message, null, _delayedTimeSpan, null, null);

      return (message.Id, message.PopReceipt);
    }

    public async Task<string> UpdateMessage(string delayedMessageId, string popReceipt)
    {
      if (string.IsNullOrWhiteSpace(delayedMessageId) || string.IsNullOrWhiteSpace(popReceipt))
        return null;

      try
      {
        var message = new CloudQueueMessage(delayedMessageId, popReceipt);

        await _timeoutQueue.UpdateMessageAsync(message, _delayedTimeSpan, MessageUpdateFields.Visibility);

        return message.PopReceipt;
      }
      catch (StorageException)
      {
        // There was a message but not any more.
        // Add timeout message;

        return null;
      }
    }

    public async Task DeleteMessage(string delayedMessageId, string popReceipt)
    {
      if (string.IsNullOrWhiteSpace(delayedMessageId) || string.IsNullOrWhiteSpace(popReceipt))
        return;

      try
      {
        await _timeoutQueue.DeleteMessageAsync(delayedMessageId, popReceipt);
      }
      catch (StorageException)
      {
        // There was a message but not any more.
      }
    }
  }
}
