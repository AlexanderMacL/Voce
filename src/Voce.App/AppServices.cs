using Voce.Core.Data;
using Voce.Core.Services;

namespace Voce.App;

/// <summary>Single place the pages get their data access from.</summary>
public static class AppServices
{
    public static VoceDatabase Database { get; private set; } = null!;
    public static ProfileRepository Profiles { get; private set; } = null!;
    public static EmployerRepository Employers { get; private set; } = null!;
    public static InvoiceService Invoices { get; private set; } = null!;
    public static SettingsRepository Settings => Invoices.Settings;

    public static void Initialize(string databasePath)
    {
        Database = new VoceDatabase(databasePath);
        Database.Initialize(seed: true);
        Profiles = new ProfileRepository(Database);
        Employers = new EmployerRepository(Database);
        Invoices = new InvoiceService(Database);
    }
}
