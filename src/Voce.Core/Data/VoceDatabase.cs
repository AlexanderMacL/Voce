using System.Reflection;
using Microsoft.Data.Sqlite;

namespace Voce.Core.Data;

/// <summary>Owns the SQLite file and hands out open connections.</summary>
public sealed class VoceDatabase
{
    public string FilePath { get; }
    private readonly string _connectionString;

    public VoceDatabase(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = FilePath,
            ForeignKeys = true,
            Pooling = false, // release the file handle so the db can be copied/backed up while the app runs
        }.ToString();
    }

    /// <summary>Default location: Data\voce.db next to the executable.</summary>
    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "Data", "voce.db");

    public SqliteConnection Open()
    {
        var c = new SqliteConnection(_connectionString);
        c.Open();
        return c;
    }

    /// <summary>
    /// Creates the file and tables if needed. Returns true when the database was brand new
    /// (no profiles, employers or invoices), in which case <paramref name="seed"/> placeholder data is added.
    /// </summary>
    public bool Initialize(bool seed = true)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        using var c = Open();
        c.Execute(ReadSchema());

        var isEmpty = c.Scalar<long>("SELECT (SELECT COUNT(*) FROM profiles) + (SELECT COUNT(*) FROM employers) + (SELECT COUNT(*) FROM invoices)") == 0;
        if (isEmpty && seed)
            PlaceholderData.Seed(this);
        return isEmpty;
    }

    public static string ReadSchema()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Voce.Core.Schema.sql")
            ?? throw new InvalidOperationException("Schema resource missing");
        using var reader = new StreamReader(s);
        return reader.ReadToEnd();
    }

    /// <summary>Copies the database file to <paramref name="destination"/> (used for backups).</summary>
    public void BackupTo(string destination)
    {
        using var src = Open();
        using var dst = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destination, Pooling = false }.ToString());
        dst.Open();
        src.BackupDatabase(dst);
    }
}
