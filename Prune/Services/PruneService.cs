using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prune.Extensions;
using Prune.Models;

namespace Prune.Services
{
    internal class PruneService(ILogger<PruneService> logger, IConfiguration configuration)
        : IPruneService
    {
        private readonly bool isDryRunEnabled = configuration.GetValue<bool>("DryRun");
        private readonly bool isForceConfirmEnabled = configuration.GetValue<bool>("ForceConfirm");
        private readonly List<string> ignoreStringsList =
            configuration.GetValue<List<string>>("IgnoreStrings") ?? [];
        private readonly int startOfWeek = configuration.GetValue<int>("StartOfWeek");
        private long previousFileLastModifiedDateInMs =
            DateTimeOffset.MaxValue.ToUnixTimeMilliseconds();
        private int currentFileIndex = 0;

        public List<PruneParameter> GetAndSetPruneConfigurations()
        {
            ignoreStringsList.AddRange(
                new List<string>()
                {
                    "appsettings.json",
                    "Bitwarden-Backup.exe",
                    "Bitwarden-Backup.pdb",
                    "bw.exe"
                }
            );

            logger.LogDebug("Getting prune configuration(s).");
            var pruneDefault =
                configuration.GetRequiredSection(PruneParameter.DefaultKey).Get<PruneParameter>()
                ?? new PruneParameter();
            var pruneParameters =
                configuration.GetRequiredSection(PruneParameter.Key).Get<List<PruneParameter>>()
                ?? [];
            logger.LogInformation("Got prune configuration(s).");

            logger.LogDebug("Setting invalid properties in prune parameter(s).");
            pruneDefault.SetInvalidProperties(
                new PruneParameter()
                {
                    KeepLast = 0,
                    KeepHourly = 0,
                    KeepDaily = 0,
                    KeepWeekly = 0,
                    KeepMonthly = 0,
                    KeepYearly = 0,
                    Path = "."
                }
            );
            pruneParameters.ForEach(
                pruneParameter => pruneParameter.SetInvalidProperties(pruneDefault)
            );
            logger.LogInformation("Set invalid properties in prune parameter(s).");

            return pruneParameters;
        }

        public void PruneDirectories(List<PruneParameter> parameters)
        {
            currentFileIndex = 0;

            foreach (var parameter in parameters)
            {
                logger.LogInformation("Pruning '{path}'.", parameter.Path);
                logger.LogDebug("Parameter:\n{@parameter}", parameter);
                var filesToRemoveList = GetFilesToRemoveList(parameter);

                logger.LogDebug("Removing {count} files(s).", filesToRemoveList.Count);
                var filesRemovedCount = RemoveFiles(filesToRemoveList, isDryRunEnabled);
                logger.LogInformation("Removed {count} file(s)", filesRemovedCount);
            }
        }

        public List<string> GetFilesToRemoveList(PruneParameter parameter)
        {
            logger.LogDebug("Checking if '{path}' exists.", parameter.Path);
            if (!Directory.Exists(parameter.Path))
            {
                logger.LogError("Directory '{path}' does not exist.", parameter.Path);
                return [];
            }

            logger.LogInformation("Getting file(s) from '{path}'.", parameter.Path);
            var filesInfo = new DirectoryInfo(parameter.Path)
                .GetFiles()
                .Where(
                    fileInfo =>
                        ignoreStringsList.Any(
                            ignoreString =>
                                !fileInfo.Name.Contains(
                                    ignoreString,
                                    StringComparison.OrdinalIgnoreCase
                                )
                        )
                )
                .ToArray();

            Array.Sort(
                filesInfo,
                (a, b) => DateTime.Compare(b.LastAccessTimeUtc, a.LastAccessTimeUtc)
            );

            logger.LogDebug("Getting files(s) to remove.");
            return GetFilesToRemoveList(parameter, filesInfo);
        }

        public int RemoveFiles(
            List<string> filePaths,
            bool isDryRunEnabled = false,
            bool isForceConfirmEnabled = false
        )
        {
            var count = 0;
            var deleteFile = true;
            foreach (var filePath in filePaths)
            {
                try
                {
                    if (isDryRunEnabled)
                    {
                        logger.LogInformation("Dry run: Deleted '{filePath}'.", filePath);
                        count++;
                        continue;
                    }

                    if (isForceConfirmEnabled)
                    {
                        Console.Write($"Are you sure you want to remove '{filePath}'? [y/n]: ");
                        deleteFile = Console.ReadLine()?.Trim().ToLower() == "y";
                    }

                    if (deleteFile)
                    {
                        File.Delete(filePath);
                        logger.LogDebug("Deleted '{filePath}'.", filePath);
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete '{filePath}'.", filePath);
                }
                finally
                {
                    deleteFile = true;
                }
            }

            return count;
        }

        private List<string> GetFilesToRemoveList(PruneParameter parameter, FileInfo[] filePaths)
        {
            var filesToRemove = new List<string>();

            filesToRemove.AddRange(
                GetFilesToRemoveForInterval(filePaths, parameter.KeepLast, Interval.Last)
            );
            filesToRemove.AddRange(
                GetFilesToRemoveForInterval(filePaths, parameter.KeepHourly, Interval.Hourly)
            );
            filesToRemove.AddRange(
                GetFilesToRemoveForInterval(filePaths, parameter.KeepDaily, Interval.Daily)
            );
            filesToRemove.AddRange(
                GetFilesToRemoveForInterval(filePaths, parameter.KeepWeekly, Interval.Weekly)
            );
            filesToRemove.AddRange(
                GetFilesToRemoveForInterval(filePaths, parameter.KeepMonthly, Interval.Monthly)
            );
            filesToRemove.AddRange(
                GetFilesToRemoveForInterval(filePaths, parameter.KeepYearly, Interval.Yearly)
            );

            return filesToRemove;
        }

        private List<string> GetFilesToRemoveForInterval(
            FileInfo[] filePaths,
            int keepCount,
            Interval interval
        )
        {
            var filesToRemove = new List<string>();
            var tempKeepCount = 0;

            if (keepCount < 1)
            {
                return filesToRemove;
            }

            while (currentFileIndex < filePaths.Length || tempKeepCount < keepCount)
            {
                var intervalStartInUnixMs = IntervalExtension.GetIntervalStartInUnixMs(
                    interval,
                    previousFileLastModifiedDateInMs,
                    0,
                    startOfWeek
                );
                var intervalEndInUnixMs =
                    IntervalExtension.GetIntervalStartInUnixMs(
                        interval,
                        intervalStartInUnixMs,
                        1,
                        startOfWeek
                    ) - 1;

                var currentFilePath = filePaths[currentFileIndex];
                var currentFileLastModifiedDateInMs = DateTimeOffset
                    .Parse(currentFilePath.LastAccessTime.ToString())
                    .ToUnixTimeMilliseconds();

                currentFileIndex++;

                if (
                    currentFileLastModifiedDateInMs <= intervalEndInUnixMs
                    && currentFileLastModifiedDateInMs >= intervalStartInUnixMs
                )
                {
                    previousFileLastModifiedDateInMs = currentFileLastModifiedDateInMs;
                    tempKeepCount++;
                    continue;
                }

                filesToRemove.Add(currentFilePath.FullName);
            }

            return filesToRemove;
        }
    }

    internal interface IPruneService
    {
        public List<PruneParameter> GetAndSetPruneConfigurations();

        public void PruneDirectories(List<PruneParameter> parameters);

        public List<string> GetFilesToRemoveList(PruneParameter parameter);

        public int RemoveFiles(
            List<string> filePaths,
            bool isDryRunEnabled = false,
            bool isForceConfirmEnabled = false
        );
    }
}
