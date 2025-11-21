using System.Globalization;

namespace Morningstar.Streaming.Client.Extensions;

public static class DateTimeExtensions
{
    private const string Iso8601TimeFormat = "O";
    public static string ToIsoFormat(this DateTime dateTime) =>
        dateTime.ToString(Iso8601TimeFormat, CultureInfo.InvariantCulture);
}
