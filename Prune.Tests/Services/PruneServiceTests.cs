using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prune.Models;
using Prune.Wrapper;

namespace Prune.Services.Tests
{
    [TestClass()]
    public class PruneServiceTests
    {
        private IConfiguration? configuration;
        private ILogger<PruneService>? logger;
        private Mock<IDirectoryInfoWrapper>? directoryInfoWrapperMock;
        private Mock<IFileWrapper>? fileWrapperMock;
        private PruneService? pruneService;

        [TestInitialize()]
        public void Initialize()
        {
            var testConfiguration = GetTestConfiguration();
            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(testConfiguration)
                .Build();
            logger = new NullLogger<PruneService>();
            directoryInfoWrapperMock = new Mock<IDirectoryInfoWrapper>();
            fileWrapperMock = new Mock<IFileWrapper>();

            pruneService = new(
                logger,
                configuration,
                directoryInfoWrapperMock.Object,
                fileWrapperMock.Object
            );
        }

        [TestMethod()]
        public void GetAndSetPruneConfigurations_ParameterWithTwoInvalidKeepValues_ReturnsCorrectedParameter()
        {
            // Default
            // { "Default:KeepLast", "0" },
            // { "Default:KeepHourly", "0" },
            // { "Default:KeepDaily", "0" },
            // { "Default:KeepWeekly", "0" },
            // { "Default:KeepMonthly", "0" },
            // { "Default:KeepYearly", "0" },

            // Parameter
            // { "PruneDirectories:0:Path", "TestFiles" },
            // { "PruneDirectories:0:FileNamePattern", "" },
            // { "PruneDirectories:0:KeepLast", "-1" },
            // { "PruneDirectories:0:KeepHourly", "3" },
            // { "PruneDirectories:0:KeepDaily", "-5" },
            // { "PruneDirectories:0:KeepWeekly", "1" },
            // { "PruneDirectories:0:KeepMonthly", "0" },
            // { "PruneDirectories:0:KeepYearly", "0" }

            // Act
            var parameters = pruneService!.GetAndSetPruneConfigurations();

            // Assert
            // Unchanged
            Assert.AreEqual("TestFiles", parameters[0].Path);
            Assert.AreEqual(string.Empty, parameters[0].FileNamePattern);
            Assert.AreEqual(3, parameters[0].KeepHourly);
            Assert.AreEqual(1, parameters[0].KeepWeekly);
            Assert.AreEqual(0, parameters[0].KeepMonthly);
            Assert.AreEqual(0, parameters[0].KeepYearly);

            // Corrected
            Assert.AreEqual(0, parameters[0].KeepDaily);
            Assert.AreEqual(0, parameters[0].KeepLast);
        }

        [TestMethod()]
        public void PruneDirectories_KeepTwoLastOneDailyOneWeeklyTwoMonthly()
        {
            // Arrange
            var path = "ValidFolder";
            var files = GetTestFiles();
            var parameters = new List<PruneParameter>()
            {
                new()
                {
                    KeepLast = 2,
                    KeepHourly = 0,
                    KeepDaily = 1,
                    KeepWeekly = 1,
                    KeepMonthly = 2,
                    KeepYearly = 0,
                    Path = path
                }
            };
            var filesToKeep = new HashSet<string>
            {
                "file_created_2023-07-28_20-00-00.txt",
                "file_created_2023-07-28_16-00-00.txt",
                "file_created_2023-07-27_20-00-00.txt",
                "file_created_2023-07-23_20-00-00.txt"
            };

            foreach (var file in files)
            {
                fileWrapperMock!.Setup(x => x.Delete(file.FullName));
            }

            directoryInfoWrapperMock!.Setup(x => x.Exists(path)).Returns(true);
            directoryInfoWrapperMock.Setup(x => x.GetFiles(path)).Returns([.. files]);

            // Act
            pruneService!.PruneDirectories(parameters);

            // Assert
            foreach (var file in files)
            {
                if (filesToKeep.Contains(file.Name))
                {
                    fileWrapperMock!.Verify(x => x.Delete(file.FullName), Times.Never);
                    continue;
                }

                fileWrapperMock!.Verify(x => x.Delete(file.FullName), Times.Once);
            }
            directoryInfoWrapperMock.Verify(x => x.Exists(path), Times.Once);
            directoryInfoWrapperMock.Verify(x => x.GetFiles(path), Times.Once);
        }

        [TestMethod()]
        public void GetFilesAndFilesToKeepList_InvalidPath_ReturnsEmpty()
        {
            // Arrange
            var path = "InvalidFolder";
            var parameter = new PruneParameter()
            {
                KeepLast = 0,
                KeepHourly = 0,
                KeepDaily = 0,
                KeepWeekly = 0,
                KeepMonthly = 0,
                KeepYearly = 0,
                Path = path
            };

            directoryInfoWrapperMock!.Setup(x => x.Exists(path)).Returns(false);

            // Act
            (var filePaths, var filesToKeep) = pruneService!.GetFilesAndFilesToKeepList(parameter);

            // Assert
            Assert.AreEqual(0, filePaths.Count);
            Assert.AreEqual(0, filesToKeep.Count);
            directoryInfoWrapperMock.Verify(x => x.Exists(path), Times.Once);
        }

        [TestMethod()]
        public void GetFilesAndFilesToKeepList_PruneAll_ReturnsAllFiles()
        {
            // Arrange
            var path = "ValidFolder";
            var files = GetTestFiles();
            var parameter = new PruneParameter()
            {
                KeepLast = 0,
                KeepHourly = 0,
                KeepDaily = 0,
                KeepWeekly = 0,
                KeepMonthly = 0,
                KeepYearly = 0,
                Path = path
            };

            directoryInfoWrapperMock!.Setup(x => x.Exists(path)).Returns(true);
            directoryInfoWrapperMock.Setup(x => x.GetFiles(path)).Returns([.. files]);

            // Act
            (var filePaths, var filesToKeep) = pruneService!.GetFilesAndFilesToKeepList(parameter);

            // Assert
            // There are 84 files
            Assert.AreEqual(84, filePaths.Count);
            Assert.AreEqual(0, filesToKeep.Count);
            directoryInfoWrapperMock.Verify(x => x.Exists(path), Times.Once);
            directoryInfoWrapperMock.Verify(x => x.GetFiles(path), Times.Once);
        }

        [TestMethod()]
        public void GetFilesAndFilesToKeepList_KeepTwoLastThreeHourlyOneWeekly_ReturnsAllButKeep()
        {
            // Arrange
            var path = "ValidFolder";
            var files = GetTestFiles();
            var parameter = new PruneParameter()
            {
                KeepLast = 2,
                KeepHourly = 3,
                KeepDaily = 0,
                KeepWeekly = 1,
                KeepMonthly = 0,
                KeepYearly = 0,
                Path = path
            };
            var expectedFilesToKeep = new HashSet<string>
            {
                "file_created_2023-07-28_20-00-00.txt",
                "file_created_2023-07-28_16-00-00.txt",
                "file_created_2023-07-28_12-00-00.txt",
                "file_created_2023-07-28_08-00-00.txt",
                "file_created_2023-07-28_04-00-00.txt",
                "file_created_2023-07-23_20-00-00.txt"
            };

            directoryInfoWrapperMock!.Setup(x => x.Exists(path)).Returns(true);
            directoryInfoWrapperMock.Setup(x => x.GetFiles(path)).Returns([.. files]);

            // Act
            (var filePaths, var actualFilesToKeep) = pruneService!.GetFilesAndFilesToKeepList(
                parameter
            );

            // Assert
            // There are 84 files
            Assert.AreEqual(84, filePaths.Count);
            Assert.IsTrue(actualFilesToKeep.SetEquals(expectedFilesToKeep));
            directoryInfoWrapperMock.Verify(x => x.Exists(path), Times.Once);
            directoryInfoWrapperMock.Verify(x => x.GetFiles(path), Times.Once);
        }

        [TestMethod()]
        public void GetFilesToRemoveList_KeepOneLastThreeDailyTwoMonthly_ReturnsAllButKeep()
        {
            // Arrange
            var path = "ValidFolder";
            var files = GetTestFiles();
            var parameter = new PruneParameter()
            {
                KeepLast = 1,
                KeepHourly = 0,
                KeepDaily = 3,
                KeepWeekly = 0,
                KeepMonthly = 2,
                KeepYearly = 0,
                Path = path
            };
            var expectedFilesToKeep = new HashSet<string>
            {
                "file_created_2023-07-28_20-00-00.txt",
                "file_created_2023-07-27_20-00-00.txt",
                "file_created_2023-07-26_20-00-00.txt",
                "file_created_2023-07-25_20-00-00.txt"
            };

            directoryInfoWrapperMock!.Setup(x => x.Exists(path)).Returns(true);
            directoryInfoWrapperMock.Setup(x => x.GetFiles(path)).Returns([.. files]);

            // Act
            (var filePaths, var actualFilesToKeep) = pruneService!.GetFilesAndFilesToKeepList(
                parameter
            );

            // Assert
            // There are 84 files
            Assert.AreEqual(84, filePaths.Count);
            Assert.IsTrue(actualFilesToKeep.SetEquals(expectedFilesToKeep));
            directoryInfoWrapperMock.Verify(x => x.Exists(path), Times.Once);
            directoryInfoWrapperMock.Verify(x => x.GetFiles(path), Times.Once);
        }

        [TestMethod()]
        public void RemoveFiles_ThreeValidPaths_ReturnsThree()
        {
            // Arrange
            var files = new List<string>
            {
                "file_created_2023-07-28_20-00-00.txt",
                "file_created_2023-07-27_20-00-00.txt",
                "file_created_2023-07-26_20-00-00.txt"
            };

            foreach (var file in files)
            {
                fileWrapperMock!.Setup(x => x.Delete(file));
            }

            // Act
            var filesRemovedCount = pruneService!.RemoveFiles(files, []);

            // Assert
            Assert.AreEqual(3, filesRemovedCount);

            foreach (var file in files)
            {
                fileWrapperMock!.Verify(x => x.Delete(file), Times.Once);
            }
        }

        private static Dictionary<string, string?> GetTestConfiguration()
        {
            return new Dictionary<string, string?>
            {
                { "Serilog:MinimumLevel", "Debug" },
                { "Serilog:WriteTo:0:Name", "File" },
                { "Serilog:WriteTo:0:Args:path", "Logs/log.txt" },
                { "Serilog:WriteTo:0:Args:rollingInterval", "Logs/log.txt" },
                { "Serilog:WriteTo:0:Args:retainedFileCountLimit", "null" },
                { "Serilog:WriteTo:0:Args:shared", "true" },
                {
                    "Serilog:WriteTo:0:Args:outputTemplate",
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                },
                { "DryRun", "false" },
                { "ForceConfirm", "false" },
                { "IgnoreStrings", "" },
                { "StartOfWeek", "1" },
                { "Default:KeepLast", "0" },
                { "Default:KeepHourly", "0" },
                { "Default:KeepDaily", "0" },
                { "Default:KeepWeekly", "0" },
                { "Default:KeepMonthly", "0" },
                { "Default:KeepYearly", "0" },
                { "PruneDirectories:0:Path", "TestFiles" },
                { "PruneDirectories:0:FileNamePattern", "" },
                { "PruneDirectories:0:KeepLast", "-1" },
                { "PruneDirectories:0:KeepHourly", "3" },
                { "PruneDirectories:0:KeepDaily", "-5" },
                { "PruneDirectories:0:KeepWeekly", "1" },
                { "PruneDirectories:0:KeepMonthly", "0" },
                { "PruneDirectories:0:KeepYearly", "0" }
            };
        }

        private static List<IFileWrapper> GetTestFiles()
        {
            var testFiles = new List<IFileWrapper>();

            for (var day = 15; day < 29; day++)
            {
                for (var hour = 0; hour < 21; hour += 4)
                {
                    var fileName = $"file_created_2023-07-{day:00}_{hour:00}-00-00.txt";
                    var lastAccessTimeUtc = new DateTime(
                        2023,
                        7,
                        day,
                        hour,
                        0,
                        0,
                        DateTimeKind.Utc
                    );
                    var lastAccessTime = lastAccessTimeUtc.ToLocalTime();
                    testFiles.Add(
                        new FileWrapper(fileName, fileName, lastAccessTime, lastAccessTimeUtc)
                    );
                }
            }

            return testFiles;
        }
    }
}
