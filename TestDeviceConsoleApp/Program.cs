using Azure.Storage.Queues;

namespace TestDeviceConsoleApp;

public static class Program
{
    private static QueueClient _queueClient;

    public static async Task Main(string[] args)
    {
        _queueClient = await createCloudQueue(isLocal: true);

        CancellationTokenSource cts = null;
        Task messageSenderTask      = null;

        while (true)
        {
            Console.WriteLine("Enter number of devices. 0 to exit");

            string input     = Console.ReadLine();
            int devicesCount = int.Parse(input);

            cts?.Cancel();
            messageSenderTask?.Wait();

            if (devicesCount <= 0) break;

            cts = new CancellationTokenSource();

            messageSenderTask = messageSender(devicesCount, cts.Token);
        }
    }

    private static async Task messageSender(int devicesCount, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            List<Task> tasks = Enumerable
                .Range(1, devicesCount)
                .Select(sendDeviceId)
                .ToList();

            tasks.Add(Task.Delay(TimeSpan.FromSeconds(10), ct));

            try
            {
                await Task.WhenAll(tasks);

                Console.WriteLine("Messages has been sent.");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("MessageSender is cancelled.");
            }
        }
    }

    private static async Task sendDeviceId(int deviceId)
    {
        await Task.Delay(Random.Shared.Next(1, 500));

        await _queueClient.SendMessageAsync(deviceId.ToString());
    }

    private static async Task<QueueClient> createCloudQueue(bool isLocal)
    {
        string storageConnString = isLocal
            ? "UseDevelopmentStorage=true"
            : Environment.GetEnvironmentVariable("CUSTOMCONNSTR_DeviceStorageConnString");

        // In the Function App, the "Microsoft.Azure.WebJobs.Extensions.Storage" package reads messages as Base64 strings using the QueueTrigger.
        var options = new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 };

        var queueClient = new QueueClient(storageConnString, queueName: "device-messages", options);

        await queueClient.CreateIfNotExistsAsync();

        return queueClient;
    }
}
