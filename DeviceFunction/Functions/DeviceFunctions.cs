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
    public async Task HandleMessages(
        [QueueTrigger(DeviceQueue)] string deviceId,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation($"{nameof(HandleMessages)} function processing Device-#{deviceId}");

        //await durableClient.SignalEntityAsync<IDeviceEntity>(getEntityId(deviceId), device => device.MessageReceived());
    }

    [Function(nameof(HandleOfflineMessages))]
    public async Task HandleOfflineMessages(
        [QueueTrigger(TimeoutQueue)] string deviceId,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation($"{nameof(HandleOfflineMessages)} function processing Device-#{deviceId}");

        //await durableClient.SignalEntityAsync<IDeviceEntity>(getEntityId(deviceId), device => device.DeviceTimeout());
    }

    [Function(nameof(HandleDeleteMessages))]
    public async Task HandleDeleteMessages(
        [QueueTrigger(DeleteQueue)] string deviceId,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation($"{nameof(HandleDeleteMessages)} function processing Device-#{deviceId}");

        //await durableClient.SignalEntityAsync<IDeviceEntity>(getEntityId(deviceId), device => device.DeleteDevice());
    }
}
