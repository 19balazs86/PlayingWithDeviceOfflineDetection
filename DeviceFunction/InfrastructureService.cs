using Azure.Storage.Queues;
using DeviceFunction.Functions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Immutable;

namespace DeviceFunction;

public sealed class InfrastructureService : BackgroundService
{
    // Somehow the Azure Function needs this queue. I do not know why it has this name
    // When I run the application, it shows the error: 'The specified queue does not exist.'
    // I used the Visual Studio's Output window, selected Service Dependencies, and found the 404 Not Found log message with this queue name
    private static readonly string _webjobsQueue = $"azure-webjobs-blobtrigger-{Environment.MachineName.ToLower()}-916312667";

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
