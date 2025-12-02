using Morningstar.Streaming.Client.Extensions;

namespace Morningstar.Streaming.Client.Helpers;

public static class DateTimeHelper
{
    private static readonly DateTimeOffset EpochDateTimeOffset = DateTimeOffset.UnixEpoch;
    
    /// <summary>
    /// Converts nanoseconds since midnight (UTC) to an ISO 8601 formatted date-time string.
    /// </summary>
    /// <param name="nanosSinceMidnight">Nanoseconds elapsed since midnight UTC</param>
    /// <returns>ISO 8601 formatted date-time string</returns>
    public static string ConvertFromNanosSinceMidnight(long nanosSinceMidnight)
    {
        var utcNowDateTicks = DateTime.UtcNow.Date.Ticks;
        var tradeExecutionTimeTicks = nanosSinceMidnight / 100;
        var executionTime = utcNowDateTicks + tradeExecutionTimeTicks;
        return new DateTime(executionTime, DateTimeKind.Utc).ToIsoFormat();
    }
    
    /// <summary>
    /// Converts nanoseconds since Unix epoch to an ISO 8601 formatted date-time string with 7-digit fractional seconds.
    /// </summary>
    /// <param name="nanosSinceEpoch">Nanoseconds elapsed since Unix epoch (January 1, 1970)</param>
    /// <returns>ISO 8601 formatted date-time string (yyyy-MM-ddTHH:mm:ss.fffffffZ)</returns>
    public static string ConvertFromEpoch(long nanosSinceEpoch)
    {
        var epochInSeconds = nanosSinceEpoch / 1_000_000_000;
        var remainingNanoseconds = nanosSinceEpoch % 1_000_000_000;

        var dateTime = EpochDateTimeOffset.AddSeconds(epochInSeconds);

        var ticks = remainingNanoseconds / 100;
        dateTime = dateTime.AddTicks(ticks);

        return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
    }
    
    /// <summary>
    /// Gets the current UTC time as milliseconds since Unix epoch.
    /// </summary>
    /// <returns>Milliseconds elapsed since Unix epoch (January 1, 1970)</returns>
    public static long MillisFromEpoch() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    
    /// <summary>
    /// Gets the current UTC time as nanoseconds since Unix epoch.
    /// </summary>
    /// <returns>Nanoseconds elapsed since Unix epoch (January 1, 1970)</returns>
    public static long NanosFromEpoch()
    {
        var utc = DateTimeOffset.UtcNow;
        return (utc.Ticks - EpochDateTimeOffset.Ticks) * 100;
    }
}
