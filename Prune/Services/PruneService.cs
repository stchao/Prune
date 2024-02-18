using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prune.Extensions;
using Prune.Models;
using Prune.Wrapper;

namespace Prune.Services
{
    public class PruneService(
        ILogger<PruneService> logger,
        IConfiguration configuration,
        IDirectoryInfoWrapper directoryInfoWrapper,
        IFileWrapper fileWrapper
    ) : IPruneService
    {
        private readonly bool isDryRunEnabled = configuration.GetValue<bool>("DryRun");
        private readonly bool isForceConfirmEnabled = configuration.GetValue<bool>("ForceConfirm");
        private readonly List<string> ignoreStringsList =
            configuration.GetValue<List<string>>("IgnoreStrings") ?? [];
        private readonly int startOfWeek = configuration.GetValue<int>("StartOfWeek");
        private long intervalStartInUnixMs = IntervalExtension.UnixMaxInMs;
        private int currentFileIndex = 0;

        public List<PruneParameter> GetAndSetPruneConfigurations()
        {
            ignoreStringsList.AddRange(
                ["appsettings.json", "Bitwarden-Backup.exe", "Bitwarden-Backup.pdb", "bw.exe"]
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
            logger?.LogInformation("Set invalid properties in prune parameter(s).");

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
                var filesRemovedCount = RemoveFiles(
                    filesToRemoveList,
                    isDryRunEnabled,
                    isForceConfirmEnabled
                );
                logger.LogInformation("Removed {count} file(s)", filesRemovedCount);
            }
        }

        public List<string> GetFilesToRemoveList(PruneParameter parameter)
        {
            logger.LogDebug("Checking if '{path}' exists.", parameter.Path);
            if (directoryInfoWrapper.Exists(parameter.Path))
            {
                logger.LogError("Directory '{path}' does not exist.", parameter.Path);
                return [];
            }

            logger.LogInformation("Getting file(s) from '{path}'.", parameter.Path);
            var filesInfoList = directoryInfoWrapper
                .GetFiles(parameter.Path)
                .Where(
                    fileInfo =>
                        !ignoreStringsList.Any(
                            ignoreString =>
                                fileInfo.Name.Contains(
                                    ignoreString,
                                    StringComparison.OrdinalIgnoreCase
                                )
                        )
                );

            if (!string.IsNullOrWhiteSpace(parameter.FileNamePattern))
            {
                filesInfoList = filesInfoList.Where(
                    fileInfo => Regex.IsMatch(fileInfo.Name, parameter.FileNamePattern)
                );
            }

            var filesInfoArray = filesInfoList.ToArray();

            Array.Sort(
                filesInfoArray,
                (a, b) => DateTime.Compare(b.LastAccessTimeUtc, a.LastAccessTimeUtc)
            );

            // Get the most recent file's modified date in case there is no KeepLast specified
            if (filesInfoArray.Length > 0)
            {
                intervalStartInUnixMs = DateTimeOffset
                    .Parse(filesInfoArray[0].LastAccessTime.ToString())
                    .ToUnixTimeMilliseconds();
            }

            logger.LogDebug("Getting files(s) to remove.");
            return GetFilesToRemoveList(parameter, filesInfoArray);
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
                        fileWrapper.Delete(filePath);
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

        private List<string> GetFilesToRemoveList(
            PruneParameter parameter,
            IFileWrapper[] filePaths
        )
        {
            var files = filePaths.Select(filePath => filePath.FullName).ToList();
            var filesToKeep = new HashSet<string>();

            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(
                    filePaths,
                    parameter.KeepLast,
                    Interval.Last,
                    filesToKeep.Count > 0
                )
            );
            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(
                    filePaths,
                    parameter.KeepHourly,
                    Interval.Hourly,
                    filesToKeep.Count > 0
                )
            );
            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(
                    filePaths,
                    parameter.KeepDaily,
                    Interval.Daily,
                    filesToKeep.Count > 0
                )
            );
            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(
                    filePaths,
                    parameter.KeepWeekly,
                    Interval.Weekly,
                    filesToKeep.Count > 0
                )
            );
            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(
                    filePaths,
                    parameter.KeepMonthly,
                    Interval.Monthly,
                    filesToKeep.Count > 0
                )
            );
            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(
                    filePaths,
                    parameter.KeepYearly,
                    Interval.Yearly,
                    filesToKeep.Count > 0
                )
            );

            files = files.Where(file => !filesToKeep.Contains(file)).ToList();

            return files;
        }

        private HashSet<string> GetFilesToKeepForInterval(
            IFileWrapper[] filePaths,
            int keepCount,
            Interval interval,
            bool isThereFilesToKeep
        )
        {
            var filesToKeep = new HashSet<string>();
            var tempKeepCount = 0;

            if (keepCount < 1)
            {
                return filesToKeep;
            }

            if (isThereFilesToKeep)
            {
                // Check if there was another Keep option run. If so, go to the end
                // of the previous date interval for the current interval (e.g. the current
                // interval is hourly so 15:36 becomes 14:59)
                intervalStartInUnixMs =
                    interval.GetIntervalStartInUnixMs(intervalStartInUnixMs, 0, startOfWeek) - 1;
            }

            while (currentFileIndex < filePaths.Length && tempKeepCount < keepCount)
            {
                intervalStartInUnixMs = interval.GetIntervalStartInUnixMs(
                    intervalStartInUnixMs,
                    0,
                    startOfWeek
                );
                var intervalEndInUnixMs =
                    interval.GetIntervalStartInUnixMs(intervalStartInUnixMs, 1, startOfWeek) - 1;

                var currentFilePath = filePaths[currentFileIndex];
                var currentFileLastModifiedDateInMs = DateTimeOffset
                    .Parse(currentFilePath.LastAccessTime.ToString())
                    .ToUnixTimeMilliseconds();

                currentFileIndex++;

                if (
                    currentFileLastModifiedDateInMs <= intervalEndInUnixMs
                    || interval.Equals(Interval.Last)
                )
                {
                    filesToKeep.Add(currentFilePath.FullName);
                    // Update previousFileLastModifiedDateInMs with the previous date interval
                    // (e.g. 15:36 becomes 14:00, or May 18th becomes May 17th) to prevent
                    // overcounting files for the same date interval
                    intervalStartInUnixMs = interval.GetIntervalStartInUnixMs(
                        currentFileLastModifiedDateInMs,
                        -1,
                        startOfWeek
                    );
                    tempKeepCount++;
                    continue;
                }
            }

            return filesToKeep;
        }
    }

    public interface IPruneService
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
