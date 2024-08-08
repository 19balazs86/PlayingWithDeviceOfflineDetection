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

    //[Function(nameof(HandleEventGridMessages))]
    //[SignalROutput(HubName = DashboardFunctions.SignalRHubName)]
    //public async Task<SignalRMessageAction> HandleEventGridMessages(
    //    [EventGridTrigger(IsBatched = false)] CloudEvent cloudEvent,
    //    [DurableClient] DurableTaskClient durableClient)
    //{
    //    // Install-Package Microsoft.Azure.Functions.Worker.Extensions.EventGrid
    //    // Event Grid binding: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-event-grid
    //    // Test locally: https://learn.microsoft.com/en-us/azure/communication-services/how-tos/event-grid/local-testing-event-grid
    //    // I am sending a number as data, but in case of string: data: "1" -> you need to deviceId.Trim('"')
    //    string deviceId = cloudEvent.Data?.ToString() ?? "n/a";

    //    _logger.LogInformation($"{nameof(HandleEventGridMessages)} function processing Device-#{deviceId}");

    //    await durableClient.Entities.SignalEntityAsync(DeviceEntity.CreateEntityId(deviceId), nameof(DeviceEntity.MessageReceived));

    //    return new SignalRMessageAction(target: "statusChanged", [new { deviceId, status = "online" }]);
    //}
}
