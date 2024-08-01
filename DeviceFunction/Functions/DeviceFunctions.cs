using DeviceFunction.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace DeviceFunction.Functions;

public sealed class DeviceFunctions
{
    public const string DeviceQueue  = "device-messages";
    public const string TimeoutQueue = "timeout-messages";
    public const string DeleteQueue  = "delete-devices";

    private readonly ILogger<DeviceFunctions> _logger;

    public DeviceFunctions(ILogger<DeviceFunctions> logger)
    {
        _logger = logger;
    }

    [Function(nameof(HandleMessages))]
    [SignalROutput(HubName = DashboardFunctions.SignalRHubName)]
    public async Task<SignalRMessageAction> HandleMessages(
        [QueueTrigger(DeviceQueue)] string deviceId,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation($"{nameof(HandleMessages)} function processing Device-#{deviceId}");

        await durableClient.Entities.SignalEntityAsync(DeviceEntity.CreateEntityId(deviceId), nameof(DeviceEntity.MessageReceived));

        return new SignalRMessageAction(target: "statusChanged", [new { deviceId, status = "online" }]);
    }

    [Function(nameof(HandleOfflineMessages))]
    [SignalROutput(HubName = DashboardFunctions.SignalRHubName)]
    public async Task<SignalRMessageAction> HandleOfflineMessages(
        [QueueTrigger(TimeoutQueue)] string deviceId,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation($"{nameof(HandleOfflineMessages)} function processing Device-#{deviceId}");

        await durableClient.Entities.SignalEntityAsync(DeviceEntity.CreateEntityId(deviceId), nameof(DeviceEntity.DeviceTimeout));

        return new SignalRMessageAction(target: "statusChanged", [new { deviceId, status = "offline" }]);
    }

    [Function(nameof(HandleDeleteMessages))]
    public async Task HandleDeleteMessages(
        [QueueTrigger(DeleteQueue)] string deviceId,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation($"{nameof(HandleDeleteMessages)} function processing Device-#{deviceId}");

        await durableClient.Entities.SignalEntityAsync(DeviceEntity.CreateEntityId(deviceId), "Delete");
    }
}
