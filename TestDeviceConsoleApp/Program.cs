using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;

namespace TestDeviceConsoleApp;

public static class Program
{
    private static CloudQueue _queue;

    public static async Task Main(string[] args)
    {
        _queue = await createCloudQueue(true);

        CancellationTokenSource cts = null;
        Task messageSenderTask = null;

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

        await _queue.AddMessageAsync(new CloudQueueMessage(deviceId.ToString()));
    }

    private static async Task<CloudQueue> createCloudQueue(bool isLocal)
    {
        string storageConnString = isLocal
            ? "UseDevelopmentStorage=true"
            : Environment.GetEnvironmentVariable("CUSTOMCONNSTR_DeviceStorageConnString");

        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnString);
        CloudQueueClient queueClient       = storageAccount.CreateCloudQueueClient();

        CloudQueue queue = queueClient.GetQueueReference("device-messages");

        await queue.CreateIfNotExistsAsync();

        return queue;
    }
}
