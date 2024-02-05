using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prune.Extensions;
using Prune.Services;

namespace Prune
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .ConfigureLogAndServices()
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILogger<Program>>();

            if (logger is null)
            {
                Console.WriteLine("Failed to get and/or initialize the logger.");
                return;
            }

            try
            {
                logger.LogDebug("Getting required service(s).");
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var pruneService = serviceProvider.GetRequiredService<IPruneService>();
                logger.LogInformation("Got required service(s).");

                logger.LogDebug("Getting prune parameter(s).");
                var pruneParameters = pruneService.GetAndSetPruneConfigurations();
                logger.LogInformation("Got prune parameter(s).");

                logger.LogDebug("Pruning file(s).");
                pruneService.PruneDirectories(pruneParameters);
                logger.LogInformation("Pruned file(s).");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Failed to get the required service(s).");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to prune file(s).");
            }
        }
    }
}
