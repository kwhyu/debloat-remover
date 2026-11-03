# DebloatManager

Remove preinstalled apps, uninstall any program, and turn off unwanted Windows components, with clear risk levels and verified results.

Windows 10 and 11, 64-bit. Free and open source under the [MIT License](LICENSE).

> [!WARNING]
> This tool makes real, often permanent changes to Windows. Removed apps lose their data, and Windows features or other apps can stop working. Read [RISKS.md](RISKS.md) and back up your files before you start.

## Important notes

- **You are responsible for what you remove.** Risk levels are guidance and can be wrong. The software comes with no warranty.
- **Use it only on your own device.** Do not use it on a work or school PC without permission.
- **Restore points are not a full undo.** They do not reliably bring back Store apps.
- **Windows may bring things back.** Major updates can reinstall some apps or reset services.
- **Administrator rights are required.** Windows asks for approval each time you open the app.
- **This is not an antivirus.** It shows where software comes from, but it does not scan for malware.
- **The downloads are not code signed.** Windows SmartScreen or your antivirus may warn you. Download only from this repository and check the checksum.
- **Nothing is collected.** The app has no telemetry, accounts or ads. See [PRIVACY.md](PRIVACY.md).

## Install

Requires 64-bit Windows 10 or 11. You do not need to install .NET.

| Method | Best for |
| --- | --- |
| [Terminal](#terminal) | The quickest install, with an automatic checksum check |
| [Installer](#installer) | A normal setup wizard |
| [Portable](#portable) | Running it without installing |
| [From source](#from-source) | Developers |

### Terminal

Open PowerShell and run:

```powershell
irm https://raw.githubusercontent.com/kwhyu/debloat-remover/main/install.ps1 | iex
```

The script shows the risks and asks you to type `ACCEPT`, downloads the latest installer, verifies its checksum, then asks for administrator approval.

To read the script before you run it:

```powershell
irm https://raw.githubusercontent.com/kwhyu/debloat-remover/main/install.ps1 -OutFile install.ps1
notepad install.ps1
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Options work with the downloaded script, for example `.\install.ps1 -Version 1.1.0`:

| Option | Purpose |
| --- | --- |
| `-AcceptEula` | Accept the EULA without the prompt, for scripted installs |
| `-Version 1.1.0` | Install a specific release |
| `-DownloadOnly` | Download and verify without installing |
| `-Uninstall` | Remove DebloatManager |

### Installer

1. Download `DebloatManagerSetup.msi` from [Releases](https://github.com/kwhyu/debloat-remover/releases/latest).
2. Open it. If SmartScreen appears, select **More info**, then **Run anyway**, only if you downloaded it from this repository.
3. Accept the license agreement and finish the wizard.

### Portable

1. Download `DebloatManager.exe` from [Releases](https://github.com/kwhyu/debloat-remover/releases/latest).
2. Run it. No installation is needed. It stores logs in `%LocalAppData%\DebloatManager`.

### From source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
git clone https://github.com/kwhyu/debloat-remover.git
cd debloat-remover
dotnet build DebloatManager.sln
dotnet run --project src/DebloatManager
```

To build the release files in `publish\`, run `installer\build-installer.ps1`.

### Verify a download

Each file has a matching `.sha256` file in the same release. Compare it with:

```powershell
Get-FileHash .\DebloatManagerSetup.msi -Algorithm SHA256
```

### Uninstall DebloatManager

Use **Settings > Apps > Installed apps**, or run `.\install.ps1 -Uninstall`. For the portable version, delete the file.

This removes DebloatManager only. It does not bring back anything you removed with it. To delete its logs too, delete `%LocalAppData%\DebloatManager`.

## Usage

1. Open DebloatManager and approve the administrator prompt. It scans automatically.
2. Tick the items you want to remove. Click an item to read what it does and how to undo it.
3. Select **Remove selected**, review the list, and confirm.

A restore point is created first unless you turn that off. An item is reported as removed only after DebloatManager checks that Windows no longer has it. If that check fails, the item is reported as failed and the reason is shown.

Start with **Select safe items**, then restart and check Windows before you remove more.

## What it does

| Item | Action | Verified by |
| --- | --- | --- |
| Store apps | Removed for all users, including the copy for new users | Package and provisioned lists |
| Programs you or your manufacturer installed | The publisher's uninstaller runs | Apps & Features entry is gone |
| Windows features | Turned off, not deleted | Feature state |
| Services | Stopped and disabled, not deleted | Startup type |

### Remaining files

When you uninstall a program, you choose what happens to its remaining files:

- **Leave them**, which is the default.
- **Delete remaining folders**, which covers the install folder and matching folders in `AppData` and `ProgramData`. This includes saved settings and cannot be undone.

The confirmation window lists each folder before anything is deleted. Folders are deleted only after the uninstall is verified. Folders that another program or Windows may use are kept.

If a program or folder cannot be removed, the app says so and shows the reason. An item that was uninstalled but still has files shows the status **Files remain**. Registry settings are not cleaned, and some files stored elsewhere can remain.

## Where software comes from

The **Origin** column helps you decide whether to keep something you do not recognize. For programs, the app checks the digital signature of the program file. The check runs offline and sends nothing anywhere.

| Origin | Meaning |
| --- | --- |
| Windows | Part of Windows |
| Store | Installed from the Microsoft Store |
| Verified | Has a valid signature, and the app shows the company that signed it |
| Unsigned | No signature, so the publisher name cannot be confirmed |
| Invalid | The signature failed, so the file may be changed or damaged, or the certificate is expired or untrusted |
| Not checked | No program file was available to check |

Select an item to see who signed it, when it was installed, where it is, and the website it lists.

> [!IMPORTANT]
> Origin is not an antivirus scan. **Verified** confirms who made a program, not that it is safe, and **Unsigned** does not mean it is harmful, because many legitimate tools are unsigned. The publisher name and website come from the program itself and can be false unless a signature confirms them. If you suspect malware, use antivirus software.

## Risk levels

| Level | Meaning |
| --- | --- |
| Safe | Preinstalled and rarely needed |
| Caution | Useful to some people, other things may rely on it |
| Unknown | Not in the catalog, check what it is first |
| Risky | Can break Windows features or other apps, requires acknowledgement |
| Protected | Required by Windows, cannot be removed here |

## Undo a change

| Item | How to get it back |
| --- | --- |
| Store app | Reinstall from the Microsoft Store |
| Program | Reinstall from the publisher |
| Windows feature | Open "Turn Windows features on or off" |
| Service | Set the startup type in `services.msc`. The previous value is in the log |
| Anything else | System Restore, or **Settings > System > Recovery** |

## Contributing

Contributions are welcome, especially new catalog entries, corrected risk levels and bug reports. Read [CONTRIBUTING.md](CONTRIBUTING.md) first. Test real removals only in a virtual machine.

Report security problems privately as described in [SECURITY.md](SECURITY.md), not in a public issue.

## License and legal

DebloatManager is released under the [MIT License](LICENSE). You may use, change and share it, including commercially, as long as you keep the copyright and license notice.

| Document | Covers |
| --- | --- |
| [EULA.md](EULA.md) | Agreement accepted when installing or first running the app |
| [TERMS.md](TERMS.md) | Acceptable use and responsibilities |
| [PRIVACY.md](PRIVACY.md) | No data collection, and what stays on your device |
| [RISKS.md](RISKS.md) | What can go wrong and how to undo changes |
| [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) | Open source components |

DebloatManager is an independent project and is not affiliated with or endorsed by Microsoft. Windows is a trademark of Microsoft Corporation.
