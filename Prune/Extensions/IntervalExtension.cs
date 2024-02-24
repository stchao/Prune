using Prune.Models;

namespace Prune.Extensions
{
    public static class IntervalExtension
    {
        public const long UnixEpochInMs = 0;
        public const long UnixMaxInMs = 253_402_300_799_000;
        public const long MsInHour = 3_600_000;
        public const long MsInDay = 86_400_000;
        public const long MsInWeek = 604_800_000;

        public static long GetIntervalStartInUnixMs(
            this Interval interval,
            long unixTimeInMs,
            int intervalToAdd = 0,
            int startOfWeek = 0
        )
        {
            var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeInMs);
            var msInInterval = MsInDay;

            switch (interval)
            {
                case Interval.Last:
                    return unixTimeInMs;
                case Interval.Hourly:
                    msInInterval = MsInHour;
                    dateTimeOffset = dateTimeOffset.AddHours(intervalToAdd);
                    break;
                case Interval.Daily:
                    dateTimeOffset = dateTimeOffset.AddDays(intervalToAdd);
                    break;
                case Interval.Weekly:
                    var dayOfWeek = (int)dateTimeOffset.DayOfWeek;
                    dayOfWeek =
                        startOfWeek > dayOfWeek
                            ? 7 - startOfWeek - dayOfWeek
                            : dayOfWeek - startOfWeek;
                    dateTimeOffset = dateTimeOffset.AddDays((intervalToAdd * 7) - dayOfWeek);
                    break;
                case Interval.Monthly:
                    dateTimeOffset = dateTimeOffset
                        .AddDays(-dateTimeOffset.Day + 1)
                        .AddMonths(intervalToAdd);
                    break;
                case Interval.Yearly:
                    dateTimeOffset = dateTimeOffset
                        .AddDays(-dateTimeOffset.DayOfYear + 1)
                        .AddYears(intervalToAdd);
                    break;
                default:
                    return -1;
            }

            return dateTimeOffset
                .AddMilliseconds(-unixTimeInMs % msInInterval)
                .ToUnixTimeMilliseconds();
        }
    }
}
