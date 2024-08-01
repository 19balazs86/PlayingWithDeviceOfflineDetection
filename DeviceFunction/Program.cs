using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DeviceFunction;

public static class Program
{
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

    }
}
