using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Prune.Services;
using Prune.Wrapper;
using Serilog;

namespace Prune.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection ConfigureLogAndServices(this IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            services
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(configure => configure.AddSerilog())
                .AddScoped<IDirectoryInfoWrapper, DirectoryInfoWrapper>()
                .AddScoped<IFileWrapper, FileWrapper>()
                .AddScoped<IPruneService, PruneService>();

            return services;
        }
    }
}
