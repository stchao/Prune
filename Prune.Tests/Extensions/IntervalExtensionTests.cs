using Prune.Models;

namespace Prune.Extensions.Tests
{
    [TestClass()]
    public class IntervalExtensionTests
    {
        // Date is 12/01/2023 15:18:11
        private const long startDateTimeInMs = 1_701_443_891_000;

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstWithLastInterval_ReturnsDateConst()
        {
            // Arrange
            var interval = Interval.Last;

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs);

            // Assert
            Assert.AreEqual(startDateTimeInMs, intervalStart);
        }

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstWithHourlyInterval_ReturnsStartOfHourOfDateConst()
        {
            // Arrange
            var interval = Interval.Hourly;
            var startOfHourInMs = 1_701_442_800_000; // 12/01/2023 15:00:00

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs);

            // Assert
            Assert.AreEqual(startOfHourInMs, intervalStart);
        }

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstAndNegativeOneIntervalToAddWithHourlyInterval_ReturnsStartOfPreviousHourOfDateConst()
        {
            // Arrange
            var interval = Interval.Hourly;
            var startOfPreviousHourInMs = 1_701_439_200_000; // 12/01/2023 14:00:00

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs, -1);

            // Assert
            Assert.AreEqual(startOfPreviousHourInMs, intervalStart);
        }

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstWithDailyInterval_ReturnsStartOfDayOfDateConst()
        {
            // Arrange
            var interval = Interval.Daily;
            var startOfDayInMs = 1_701_388_800_000; // 12/01/2023 00:00:00

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs);

            // Assert
            Assert.AreEqual(startOfDayInMs, intervalStart);
        }

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstAndNegativeOneIntervalToAddWithDailyInterval_ReturnsStartOfPreviousDayOfDateConst()
        {
            // Arrange
            var interval = Interval.Daily;
            var startOfPreviousDayInMs = 1_701_302_400_000; // 11/30/2023 00:00:00

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs, -1);

            // Assert
            Assert.AreEqual(startOfPreviousDayInMs, intervalStart);
        }

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstWithWeeklyIntervalStartingMonday_ReturnsStartOfWeekOfDateConst()
        {
            // Arrange
            var interval = Interval.Weekly;
            var startOfWeekIfMondayInMs = 1_701_043_200_000; // 11/27/2023 00:00:00

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs, 0, 1);

            // Assert
            Assert.AreEqual(startOfWeekIfMondayInMs, intervalStart);
        }

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstAndNegativeOneIntervalToAddWithWeeklyIntervalStartingMonday_ReturnsStartOfPreviousWeekOfDateConst()
        {
            // Arrange
            var interval = Interval.Weekly;
            var startOfPreviousWeekIfMondayInMs = 1_700_438_400_000; // 11/20/2023 00:00:00

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs, -1, 1);

            // Assert
            Assert.AreEqual(startOfPreviousWeekIfMondayInMs, intervalStart);
        }

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstWithWeeklyIntervalStartingSunday_ReturnsStartOfWeekOfDateConst()
        {
            // Arrange
            var interval = Interval.Weekly;
            var startOfWeekIfSundayInMs = 1_700_956_800_000; // 11/26/2023 00:00:00

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs);

            // Assert
            Assert.AreEqual(startOfWeekIfSundayInMs, intervalStart);
        }

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstWithMonthlyInterval_ReturnsStartOfMonthOfDateConst()
        {
            // Arrange
            var interval = Interval.Monthly;
            var startOfMonthInMs = 1_701_388_800_000; // 12/01/2023 00:00:00

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs);

            // Assert
            Assert.AreEqual(startOfMonthInMs, intervalStart);
        }

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstAndNegativeOneIntervalToAddWithMonthlyInterval_ReturnsStartOfPreviousMonthOfDateConst()
        {
            // Arrange
            var interval = Interval.Monthly;
            var startOfPreviousMonthInMs = 1_698_796_800_000; // 11/01/2023 00:00:00

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs, -1);

            // Assert
            Assert.AreEqual(startOfPreviousMonthInMs, intervalStart);
        }

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstWithYearlyInterval_ReturnsStartOfYearOfDateConst()
        {
            // Arrange
            var interval = Interval.Yearly;
            var startOfYearInMs = 1_672_531_200_000; // 01/01/2023 00:00:00

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs, 0, 1);

            // Assert
            Assert.AreEqual(startOfYearInMs, intervalStart);
        }

        [TestMethod()]
        public void GetIntervalStartInUnixMs_DateConstAndNegativeOneIntervalToAddWithYearlyInterval_ReturnsStartOfPreviousYearOfDateConst()
        {
            // Arrange
            var interval = Interval.Yearly;
            var startOfPreviousYearInMs = 1_640_995_200_000; // 01/01/2022 00:00:00

            // Act
            var intervalStart = interval.GetIntervalStartInUnixMs(startDateTimeInMs, -1, 1);

            // Assert
            Assert.AreEqual(startOfPreviousYearInMs, intervalStart);
        }
    }
}
