using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Voce.Core.Data;

/// <summary>Small ADO.NET helpers so the repositories stay readable.</summary>
internal static class Db
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

    public static SqliteCommand Command(this SqliteConnection c, string sql, params (string Name, object? Value)[] args)
    {
        var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    public static int Execute(this SqliteConnection c, string sql, params (string, object?)[] args)
    {
        using var cmd = c.Command(sql, args);
        return cmd.ExecuteNonQuery();
    }

    public static long Insert(this SqliteConnection c, string sql, params (string, object?)[] args)
    {
        using var cmd = c.Command(sql + "; SELECT last_insert_rowid();", args);
        return (long)cmd.ExecuteScalar()!;
    }

    public static T? Scalar<T>(this SqliteConnection c, string sql, params (string, object?)[] args)
    {
        using var cmd = c.Command(sql, args);
        var v = cmd.ExecuteScalar();
        if (v is null or DBNull) return default;
        return (T)Convert.ChangeType(v, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
    }

    public static List<T> Query<T>(this SqliteConnection c, string sql, Func<SqliteDataReader, T> map, params (string, object?)[] args)
    {
        using var cmd = c.Command(sql, args);
        using var r = cmd.ExecuteReader();
        var list = new List<T>();
        while (r.Read()) list.Add(map(r));
        return list;
    }

    public static string Str(this SqliteDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? "" : r.GetString(r.GetOrdinal(col));

    public static string? NStr(this SqliteDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetString(r.GetOrdinal(col));

    public static long Long(this SqliteDataReader r, string col) => r.GetInt64(r.GetOrdinal(col));

    public static long? NLong(this SqliteDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetInt64(r.GetOrdinal(col));

    public static bool Bool(this SqliteDataReader r, string col) => r.GetInt64(r.GetOrdinal(col)) != 0;

    public static DateOnly Date(this SqliteDataReader r, string col) => ParseDate(r.GetString(r.GetOrdinal(col)));

    public static DateOnly? NDate(this SqliteDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? null : ParseDate(r.GetString(r.GetOrdinal(col)));

    public static DateTime DateTime(this SqliteDataReader r, string col) =>
        System.DateTime.Parse(r.GetString(r.GetOrdinal(col)), CultureInfo.InvariantCulture);

    public static DateOnly ParseDate(string s) =>
        DateOnly.ParseExact(s.Length > 10 ? s[..10] : s, DateFormat, CultureInfo.InvariantCulture);

    public static string ToDb(this DateOnly d) => d.ToString(DateFormat, CultureInfo.InvariantCulture);
    public static string? ToDb(this DateOnly? d) => d?.ToString(DateFormat, CultureInfo.InvariantCulture);
    public static string ToDb(this DateTime d) => d.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
}
