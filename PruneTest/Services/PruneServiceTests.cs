using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Prune.Models;
using Prune.Services;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;

namespace PruneTest.Services
{
    public class PruneServiceTests
    {
        private static readonly IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        private static readonly ILogger<PruneService> logger = new NullLogger<PruneService>();
        private readonly PruneService pruneService = new(logger, configuration);
    }
}
