# Voce

Invoice generator for a freelance singer — a portable Windows 11 app (WinUI 3).

- Employers with their events and default fees
- Several sets of your own address/bank details, one default
- Multi-line PDF invoices numbered `yyyy-MM-dd-NNN`
- Amend (overwrite or save as new number), regenerate, mark paid, delete
- Dashboard for the UK tax year (6 April – 5 April) and CSV export

See [plan.md](plan.md) for the design and database schema.

## Get the app

Every push builds the app on GitHub Actions. Open the latest run of the **build** workflow, download the
`Voce-win-x64` artifact, unzip it anywhere (e.g. a OneDrive or USB folder) and run `Voce.exe`.
The database is created at `Data\voce.db` beside the exe and PDFs go to `Invoices\` by default.
Keep the folder somewhere you can write to (not `C:\Program Files`).

## Build locally (Windows)

Requires the .NET 8 SDK and Visual Studio (or Build Tools) with the *Windows application development* workload.
Use MSBuild for the app — the WinUI XAML compiler fails silently under `dotnet build`/`dotnet publish`.
Run from a *Developer PowerShell*:

```
dotnet test tests/Voce.Core.Tests
msbuild src/Voce.App/Voce.App.csproj /restore /t:Publish /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:PublishDir=publish\Voce\
```

Or open `Voce.sln` in Visual Studio, set the platform to x64 and run `Voce.App`.

`Voce.Core` (database, numbering, PDF, CSV) is plain .NET 8 and builds and tests on any OS.
