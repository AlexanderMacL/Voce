using Voce.Core.Data;

namespace Voce.Core.Tests;

/// <summary>A throwaway database + PDF folder in the temp directory.</summary>
public sealed class TestDb : IDisposable
{
    public string Folder { get; } = Path.Combine(Path.GetTempPath(), "voce-tests-" + Guid.NewGuid().ToString("N"));
    public VoceDatabase Db { get; }

    public TestDb(bool seed = false)
    {
        Directory.CreateDirectory(Folder);
        Db = new VoceDatabase(Path.Combine(Folder, "voce.db"));
        Db.Initialize(seed);
        new SettingsRepository(Db).PdfFolder = Path.Combine(Folder, "pdf");
    }

    public void Dispose()
    {
        try { Directory.Delete(Folder, recursive: true); } catch { }
    }
}
