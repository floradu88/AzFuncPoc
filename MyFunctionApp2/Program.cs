using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureFunctionsWorkerDefaults((IFunctionsWorkerApplicationBuilder
        workerOptions) =>
    {
        //workerOptions.AddServiceBus();
        //workerOptions.AddTimers();
    })
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Use connection string from configuration
        string connectionString = context.Configuration["BlobStorageConnectionString"];
        services.AddSingleton(new BlobServiceClient(connectionString));
    })
    .Build();

host.Run();