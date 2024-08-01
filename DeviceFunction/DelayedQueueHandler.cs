using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace DeviceFunction;

public interface IDelayedQueueHandler
{
    Task<(string messageId, string popReceipt)> SendMessage(string message);

    Task<string?> UpdateMessage(string? messageId, string? popReceipt);

    Task DeleteMessage(string? messageId, string? popReceipt);
}

public sealed class DelayedQueueHandler : IDelayedQueueHandler
{
    private readonly QueueClient _queueClient;
    private readonly TimeSpan _visibilityTimeout;

    public DelayedQueueHandler(QueueClient queueClient, TimeSpan visibilityTimeout)
    {
        _queueClient       = queueClient;
        _visibilityTimeout = visibilityTimeout;
    }

    public async Task<(string messageId, string popReceipt)> SendMessage(string message)
    {
        SendReceipt response = await _queueClient.SendMessageAsync(message, _visibilityTimeout);

        return (response.MessageId, response.PopReceipt);
    }

    public async Task<string?> UpdateMessage(string? messageId, string? popReceipt)
    {
        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(popReceipt))
        {
            return null;
        }

        try
        {
            UpdateReceipt response = await _queueClient.UpdateMessageAsync(messageId, popReceipt, visibilityTimeout: _visibilityTimeout);

            return response.PopReceipt;
        }
        catch (RequestFailedException)
        {
            // There was a message but not any more. Add timeout message.

            return null;
        }
    }

    public async Task DeleteMessage(string? messageId, string? popReceipt)
    {
        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(popReceipt))
        {
            return;
        }

        try
        {
            await _queueClient.DeleteMessageAsync(messageId, popReceipt);
        }
        catch (RequestFailedException)
        {
            // There was a message but not any more.
        }
    }
}
