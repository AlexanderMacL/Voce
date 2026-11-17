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

Requires the .NET 8 SDK. Visual Studio 2022 with the *Windows application development* workload is optional.

```
dotnet test tests/Voce.Core.Tests
dotnet publish src/Voce.App/Voce.App.csproj -c Release -r win-x64 -p:Platform=x64 -o publish/Voce
```

`Voce.Core` (database, numbering, PDF, CSV) is plain .NET 8 and builds and tests on any OS.
