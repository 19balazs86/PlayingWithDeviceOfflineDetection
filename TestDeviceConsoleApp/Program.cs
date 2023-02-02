using Azure.Storage.Queues;
using System.Text;

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

        // In the FunctionApp the package "Microsoft.Azure.WebJobs.Extensions.Storage" read the message in Base64 string with QueueTrigger
        string base64DevideId = Convert.ToBase64String(Encoding.UTF8.GetBytes(deviceId.ToString()));

        await _queueClient.SendMessageAsync(base64DevideId);
    }

    private static async Task<QueueClient> createCloudQueue(bool isLocal)
    {
        string storageConnString = isLocal
            ? "UseDevelopmentStorage=true"
            : Environment.GetEnvironmentVariable("CUSTOMCONNSTR_DeviceStorageConnString");

        var queueClient = new QueueClient(storageConnString, queueName: "device-messages");

        await queueClient.CreateIfNotExistsAsync();

        return queueClient;
    }
}
