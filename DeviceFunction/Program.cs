using Azure.Storage.Queues;
using DeviceFunction.Core;
using DeviceFunction.Functions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DeviceFunction;

public static class Program
{
    public const string TimeoutQueueHandlerKey = "timeoutQueueHandler";
    public const string DeleteQueueHandlerKey  = "deleteQueueHandler";

    public static void Main(string[] args)
    {
        IHost host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices(configureServices)
            .Build();

        host.Run();
    }

    private static void configureServices(HostBuilderContext builderContext, IServiceCollection services)
    {
        services.AddHostedService<InfrastructureService>();

        string storageConnString = builderContext.Configuration["AzureWebJobsStorage"]
            ?? throw new NullReferenceException("Missing configration value for AzureWebJobsStorage");

        // Azure .NET SDKs
        // https://azure.github.io/azure-sdk/releases/latest/dotnet.html

        // Example: https://kaylumah.nl/2022/02/21/working-with-azure-sdk-for-dotnet.html
        // For DI container using the package: Microsoft.Extensions.Azure

        services.AddAzureClients(clients =>
        {
            // Messages need to be encoded as Base64 because the QueueTrigger functions that way
            clients.AddQueueServiceClient(storageConnString)
                .ConfigureOptions(options => options.MessageEncoding = QueueMessageEncoding.Base64);

            // You can use the Service URL with DefaultAzureCredential, if you set the role assignment for the SystemManagedIdentity of the function
            // clients.AddQueueServiceClient(new Uri("https://<StorageAccountName>.queue.core.windows.net")).WithCredential(new DefaultAzureCredential());
        });

        services.addDelayedQueueHandler(DeviceFunctions.TimeoutQueue, TimeoutQueueHandlerKey, TimeSpan.FromSeconds(30));
        services.addDelayedQueueHandler(DeviceFunctions.DeleteQueue,  DeleteQueueHandlerKey,  TimeSpan.FromSeconds(60));
    }

    private static void addDelayedQueueHandler(this IServiceCollection services, string queueName, string serviceKey, TimeSpan visibilityTimeout)
    {
        services.AddKeyedScoped<IDelayedQueueHandler>(serviceKey, (sp, _) =>
        {
            QueueServiceClient queueServiceClient = sp.GetRequiredService<QueueServiceClient>();

            QueueClient queueClient = queueServiceClient.GetQueueClient(queueName);

            return new DelayedQueueHandler(queueClient, visibilityTimeout);
        });
    }
}
