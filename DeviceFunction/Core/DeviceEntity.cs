using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeviceFunction.Core;

public class DeviceEntity : TaskEntity<DeviceEntityState>
{
    [Function(nameof(DeviceEntity))]
    public static Task Run([EntityTrigger] TaskEntityDispatcher ctx)
    {
        return ctx.DispatchAsync<DeviceEntity>();
    }

    private string _deviceId => Context.Id.Key;

    private readonly ILogger<DeviceEntity> _logger;

    private readonly IDelayedQueueHandler _timeoutQueueHandler;
    private readonly IDelayedQueueHandler _deleteQueueHandler;

    public DeviceEntity(
        ILogger<DeviceEntity> logger,
        [FromKeyedServices(Program.TimeoutQueueHandlerKey)] IDelayedQueueHandler timeoutQueueHandler,
        [FromKeyedServices(Program.DeleteQueueHandlerKey)]  IDelayedQueueHandler deleteQueueHandler)
    {
        _logger = logger;

        _timeoutQueueHandler = timeoutQueueHandler;
        _deleteQueueHandler  = deleteQueueHandler;
    }

    public async Task MessageReceived()
    {
        State.TimeoutMessagePopReceipt = await _timeoutQueueHandler.UpdateMessage(State.TimeoutMessageId, State.TimeoutMessagePopReceipt);

        if (State.TimeoutMessagePopReceipt is null)
        {
            (State.TimeoutMessageId, State.TimeoutMessagePopReceipt) = await _timeoutQueueHandler.SendMessage(_deviceId);
        }

        await _deleteQueueHandler.DeleteMessage(State.DeleteMessageId, State.DeleteMessagePopReceipt);

        State.DeleteMessageId = State.DeleteMessagePopReceipt = null;
    }

    public async Task DeviceTimeout()
    {
        State.TimeoutMessageId = State.TimeoutMessagePopReceipt = null;

        State.DeleteMessagePopReceipt = await _deleteQueueHandler.UpdateMessage(State.DeleteMessageId, State.DeleteMessagePopReceipt);

        if (State.DeleteMessagePopReceipt is null)
        {
            (State.DeleteMessageId, State.DeleteMessagePopReceipt) = await _deleteQueueHandler.SendMessage(_deviceId);
        }
    }

    public static EntityInstanceId CreateEntityId(string deviceId)
    {
        return new EntityInstanceId(nameof(DeviceEntity), deviceId);
    }
}

public sealed class DeviceEntityState
{
    public string? TimeoutMessageId { get; set; }

    public string? TimeoutMessagePopReceipt { get; set; }

    public string? DeleteMessageId { get; set; }

    public string? DeleteMessagePopReceipt { get; set; }
}