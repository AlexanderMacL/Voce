namespace Voce.Core.Data;

public sealed class SettingsRepository(VoceDatabase db)
{
    public const string PdfFolderKey = "pdf_folder";
    public const string FileNamePatternKey = "file_name_pattern";
    public const string DefaultFileNamePattern = "Invoice-{EMPLOYER}-{NAME}";

    public static string DefaultPdfFolder => Path.Combine(AppContext.BaseDirectory, "Invoices");

    public string? Get(string key)
    {
        using var c = db.Open();
        return c.Scalar<string>("SELECT value FROM settings WHERE key = $k", ("$k", key));
    }

    public void Set(string key, string value)
    {
        using var c = db.Open();
        c.Execute("INSERT INTO settings (key, value) VALUES ($k, $v) ON CONFLICT(key) DO UPDATE SET value = excluded.value",
            ("$k", key), ("$v", value));
    }

    /// <summary>The PDF output folder. Relative paths are resolved against the exe folder.</summary>
    public string PdfFolder
    {
        get
        {
            var v = Get(PdfFolderKey);
            if (string.IsNullOrWhiteSpace(v)) return DefaultPdfFolder;
            return Path.IsPathRooted(v) ? v : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, v));
        }
        set => Set(PdfFolderKey, value);
    }

    public string FileNamePattern
    {
        get { var v = Get(FileNamePatternKey); return string.IsNullOrWhiteSpace(v) ? DefaultFileNamePattern : v; }
        set => Set(FileNamePatternKey, value);
    }
}
