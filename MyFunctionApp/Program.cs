using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using MyFunctionApp.Helpers;

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
        // Get Key Vault URI
        string keyVaultUri = context.Configuration["KeyVaultUri"];

        // Register KeyVaultHelper
        var keyVaultHelper = new KeyVaultHelper(keyVaultUri);
        services.AddSingleton(keyVaultHelper);

        // Register BlobServiceClient
        string blobConnectionString = keyVaultHelper.GetSecretAsync("BlobStorageConnectionString").Result;
        services.AddSingleton(new BlobServiceClient(blobConnectionString));

        // Register Func<string, Task<string>> for lazy secret retrieval
        services.AddSingleton<Func<string, Task<string>>>(keyVaultHelper.GetSecretAsync);
    })
    .Build();

host.Run();