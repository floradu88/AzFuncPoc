using System;
using Microsoft.Extensions.Hosting;

namespace MyFunctionApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Build and run the host
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults() // No need to add timers explicitly
                .Build();

            host.Run();
        }
    }
}