using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Voce.App.Helpers;
using Voce.Core.Data;
using Windows.Storage.Pickers;

namespace Voce.App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) => Load();

    private void Load()
    {
        PdfFolderBox.Text = AppServices.Settings.PdfFolder;
        PatternBox.Text = AppServices.Settings.FileNamePattern;
        DbPathText.Text = AppServices.Database.FilePath;
    }

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add("*");
        Ui.AttachToWindow(picker);
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null) PdfFolderBox.Text = folder.Path;
    }

    private void OpenPdfFolder_Click(object sender, RoutedEventArgs e) => Ui.OpenFolder(AppServices.Settings.PdfFolder);

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e) =>
        Ui.OpenFolder(Path.GetDirectoryName(AppServices.Database.FilePath)!);

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = PdfFolderBox.Text.Trim();
            AppServices.Settings.PdfFolder = string.IsNullOrEmpty(folder) ? SettingsRepository.DefaultPdfFolder : folder;
            AppServices.Settings.FileNamePattern = string.IsNullOrWhiteSpace(PatternBox.Text)
                ? SettingsRepository.DefaultFileNamePattern
                : PatternBox.Text.Trim();
            Load();
            await Ui.Message(XamlRoot, "Saved", "Settings saved. They apply to PDFs generated from now on.");
        }
        catch (Exception ex) { await Ui.Error(XamlRoot, ex); }
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedFileName = $"voce-backup-{DateTime.Now:yyyy-MM-dd}",
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            };
            picker.FileTypeChoices.Add("SQLite database", new List<string> { ".db" });
            Ui.AttachToWindow(picker);
            var file = await picker.PickSaveFileAsync();
            if (file is null) return;
            // The picker creates an empty file; the SQLite backup API overwrites it.
            AppServices.Database.BackupTo(file.Path);
            await Ui.Message(XamlRoot, "Backed up", $"Database copied to\n{file.Path}");
        }
        catch (Exception ex) { await Ui.Error(XamlRoot, ex); }
    }
}
