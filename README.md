# DebloatManager

A Windows 10/11 desktop utility that detects preinstalled bloatware, optional system features, and background services, then lets you end their running tasks or remove them — with risk-level warnings and an optional System Restore Point before high-risk actions.

## Features

- Detects UWP apps, classic Win32 programs, Windows optional features, and known telemetry services — using the same sources as Control Panel (registry Uninstall keys) and the Appx/DISM APIs, so every installed item shows up automatically.
- Flags each item with a risk level (Safe / Caution / Risky / Unknown): matched first against a curated bloatware catalog, then against automatic fallback rules (non-removable packages, system-signed packages, shared runtimes/redistributables) so items don't need to be hand-listed to get a reasonable risk color.
- Bulk actions: select multiple items and End Task or Remove them together, with one combined risk warning and restore-point offer.
- Group by Category, search/filter by type and risk.
- A "Running Processes" tab mirrors Task Manager's process list (name, PID, memory, path) with End Task for any process, not just cataloged apps.
- Creates a System Restore Point before risky removals.
- Persistent activity log, written to disk under `%LocalAppData%\DebloatManager\logs`, in addition to the in-app log panel.
- Fluent, Task-Manager-style UI (WPF + WPF-UI).

## Requirements

- Windows 10 or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Administrator privileges (the app requests elevation automatically)

## Build & Run

```powershell
dotnet build src/DebloatManager/DebloatManager.csproj
dotnet run --project src/DebloatManager/DebloatManager.csproj
```

The app always requests administrator elevation on launch (`app.manifest`) — this is required, not optional, since removing system components, disabling services/features, and creating restore points all need admin privileges.

## Build the Installer

```powershell
installer\build-installer.ps1
```

This publishes a self-contained release build (no .NET runtime required on the target machine) and packages it into `publish\DebloatManagerSetup.msi` using the [WiX Toolset](https://wixtoolset.org/) (installed automatically as a dotnet tool if missing). The installer shows a license/privacy notice, lets you pick the install directory, adds Start Menu and Desktop shortcuts with the app icon, and registers a normal uninstall entry under Settings → Apps. The installed app still prompts for administrator elevation on every launch, same as running it directly.

## Disclaimer

Removing system components or Windows optional features can make Windows behave unexpectedly or break other applications. Review each item's risk level before removing it, and use the built-in Restore Point option before performing risky actions. Use at your own risk. See [TERMS.md](TERMS.md) and [PRIVACY.md](PRIVACY.md) for the full terms and privacy policy.
