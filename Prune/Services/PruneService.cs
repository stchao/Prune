using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prune.Extensions;
using Prune.Models;

namespace Prune.Services
{
    public class PruneService(ILogger<PruneService> logger, IConfiguration configuration)
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
            var filesInfoList = new DirectoryInfo(parameter.Path)
                .GetFiles()
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
                previousFileLastModifiedDateInMs = DateTimeOffset
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
            var files = filePaths.Select(filePath => filePath.FullName).ToList();
            var filesToKeep = new HashSet<string>();

            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(filePaths, parameter.KeepLast, Interval.Last)
            );
            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(filePaths, parameter.KeepHourly, Interval.Hourly)
            );
            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(filePaths, parameter.KeepDaily, Interval.Daily)
            );
            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(filePaths, parameter.KeepWeekly, Interval.Weekly)
            );
            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(filePaths, parameter.KeepMonthly, Interval.Monthly)
            );
            filesToKeep.UnionWith(
                GetFilesToKeepForInterval(filePaths, parameter.KeepYearly, Interval.Yearly)
            );

            files = files.Where(file => !filesToKeep.Contains(file)).ToList();

            return files;
        }

        private HashSet<string> GetFilesToKeepForInterval(
            FileInfo[] filePaths,
            int keepCount,
            Interval interval
        )
        {
            var filesToKeep = new HashSet<string>();
            var tempKeepCount = 0;

            if (keepCount < 1)
            {
                return filesToKeep;
            }

            while (currentFileIndex < filePaths.Length && tempKeepCount < keepCount)
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
                    filesToKeep.Add(currentFilePath.FullName);
                    // Update previousFileLastModifiedDateInMs with the previous date interval
                    // (e.g. 15:36 becomes 14:36, or May 18th becomes May 17th) to prevent counting multiple files
                    // for the same interval
                    previousFileLastModifiedDateInMs = IntervalExtension.GetIntervalStartInUnixMs(
                        interval,
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
