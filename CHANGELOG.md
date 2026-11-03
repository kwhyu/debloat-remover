# Changelog

## 1.1.0

### Fixed

- MSI programs are now uninstalled. The old `msiexec /I` command silently repaired them instead.
- Uninstall commands with unquoted paths containing spaces now run.
- Items are reported as removed only after Windows confirms it.
- Store apps are removed from the copy for new users, so they no longer come back.
- Restore points are verified, and no longer report success when Windows skips them.
- Risk levels no longer match unrelated programs, such as any program with "Wallet" in its name being Safe.
- Names containing quotes can no longer inject PowerShell commands.
- Scans no longer fail when a list contains one item.

### Added

- Terminal installer, `install.ps1`, with EULA prompt and checksum check.
- Portable `DebloatManager.exe` release file.
- EULA, risk document, MIT License and third-party notices, readable inside the app.
- First-run acceptance and a confirmation window that lists each item's risk and how to undo it.
- Protected level for packages Windows needs.
- Origin column that checks digital signatures offline, and shows who signed a program, when it was installed and where it is.
- Optional deletion of remaining program folders, listed for review first, with a clear report of anything that could not be deleted.
- Automated tests.

### Changed

- Simpler gray interface with one list and one main action.
- Removed the Running Processes tab, End Task buttons and category grouping.
- Log files older than 30 days are deleted automatically.

## 1.0.0

- Initial version.
