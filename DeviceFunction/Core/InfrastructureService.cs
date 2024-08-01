using Azure.Storage.Queues;
using DeviceFunction.Functions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Immutable;

namespace DeviceFunction.Core;

public sealed class InfrastructureService : BackgroundService
{
    private readonly ImmutableArray<string> _queueNames = [DeviceFunctions.TimeoutQueue, DeviceFunctions.DeviceQueue, DeviceFunctions.DeleteQueue];

    private readonly IServiceScopeFactory _serviceScopeFactory;

    public InfrastructureService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        IServiceProvider serviceProvider = scope.ServiceProvider;

        await ensureQueueExists(serviceProvider);
    }

    private async Task ensureQueueExists(IServiceProvider serviceProvider)
    {
        QueueServiceClient queueServiceClient = serviceProvider.GetRequiredService<QueueServiceClient>();

        foreach (string queueName in _queueNames)
        {
            QueueClient queueClient = queueServiceClient.GetQueueClient(queueName);

            await queueClient.CreateIfNotExistsAsync();
        }
    }
}
