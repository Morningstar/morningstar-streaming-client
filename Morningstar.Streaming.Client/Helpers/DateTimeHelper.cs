using Morningstar.Streaming.Client.Extensions;

namespace Morningstar.Streaming.Client.Helpers;

public static class DateTimeHelper
{
    private static readonly DateTimeOffset EpochDateTimeOffset = DateTimeOffset.UnixEpoch;
    public static string ConvertFromNanosSinceMidnight(long nanosSinceMidnight)
    {
        var utcNowDateTicks = DateTime.UtcNow.Date.Ticks;
        var tradeExecutionTimeTicks = nanosSinceMidnight / 100;
        var executionTime = utcNowDateTicks + tradeExecutionTimeTicks;
        return new DateTime(executionTime, DateTimeKind.Utc).ToIsoFormat();
    }
    public static string ConvertFromEpoch(long nanosSinceEpoch)
    {
        var epochInSeconds = nanosSinceEpoch / 1000000000;
        var remainingNanoseconds = nanosSinceEpoch % 1000000000;

        var dateTime = EpochDateTimeOffset.AddSeconds(epochInSeconds);

        var ticks = remainingNanoseconds / 100;
        dateTime = dateTime.AddTicks(ticks);

        return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
    }
    public static long MillisFromEpoch() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public static long NanosFromEpoch()
    {
        var utc = DateTimeOffset.UtcNow;
        return (utc.Ticks - EpochDateTimeOffset.Ticks) * 100;
    }
}
