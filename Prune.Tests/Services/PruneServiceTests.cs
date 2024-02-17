using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Prune.Models;
using Prune.Services;

namespace Prune.Services.Tests
{
    [TestClass()]
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
