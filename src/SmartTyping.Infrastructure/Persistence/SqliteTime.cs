using System.Globalization;

namespace SmartTyping.Infrastructure.Persistence;

/// <summary>
/// Single source of truth for how timestamps are stored in SQLite. We persist UTC clock values
/// using an ISO-8601 string <b>without</b> a zone designator. A trailing 'Z' (or offset) would
/// cause the ADO.NET/Dapper reader to convert the value to local time on read, which changes the
/// <see cref="DateTime.Kind"/> and the clock value; omitting it keeps the round-trip stable.
/// Callers pass UTC values (e.g. <c>IDateTimeProvider.UtcNow</c>); columns read back as the same
/// UTC clock value (Kind = Unspecified).
/// </summary>
internal static class SqliteTime
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffffff";

    public static string ToStorage(DateTime utc) => utc.ToString(Format, CultureInfo.InvariantCulture);
}
