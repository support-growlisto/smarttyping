using System.Data;
using System.Globalization;
using Dapper;

namespace SmartTyping.Infrastructure.Persistence;

/// <summary>
/// One-time Dapper configuration. Registers a <see cref="DateTime"/> type handler so values stored
/// by <see cref="SqliteTime"/> (UTC clock, no zone designator) are materialized back with
/// <see cref="DateTimeKind.Utc"/> instead of <see cref="DateTimeKind.Unspecified"/> — keeping the
/// <c>...Utc</c> entity fields honest about their kind.
/// </summary>
internal static class DapperConfiguration
{
    private static readonly object Gate = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Gate)
        {
            if (_registered)
            {
                return;
            }

            SqlMapper.AddTypeHandler(new UtcDateTimeHandler());
            _registered = true;
        }
    }

    private sealed class UtcDateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value)
        {
            var dt = value switch
            {
                DateTime d => d,
                string s => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.None),
                _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture)
            };

            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = SqliteTime.ToStorage(value);
            parameter.DbType = DbType.String;
        }
    }
}
