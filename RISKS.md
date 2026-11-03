# Risks

DebloatManager makes real, often permanent changes to Windows. Read this before you use it.

## Risk levels in the app

- **Safe**: preinstalled and rarely needed. Removal is not expected to affect Windows.
- **Caution**: useful to some people. Other apps or Windows features may rely on it.
- **Unknown**: not in the catalog, usually a program you or your manufacturer installed. Find out what it is first.
- **Risky**: Windows features or other apps can stop working. You must acknowledge this before continuing.
- **Protected**: required by Windows. The app refuses to remove it.

Risk levels are guidance from a catalog and simple rules. They can be wrong or out of date.

## Origin is not a safety check

The Origin column shows whether a program's digital signature is valid. A valid signature confirms who signed the file, not that the program is safe. Unsigned programs are not necessarily harmful. Publisher names and websites come from the program itself. DebloatManager does not scan for malware.

## What each action does

- **Store apps** are removed for every user on the device, with their app data. The installed copy for new users is removed as well, and a marker tells Windows not to bring the app back in feature updates.
- **System packages** that Windows marks as non-removable and that are not Protected are removed using an end-of-life registry marker. This is not a supported Microsoft procedure and can cause problems with future Windows updates.
- **Programs** are removed by their publisher's own uninstaller. Running copies may be closed without saving. Remaining files are left in place unless you choose to delete them.
- **Deleting remaining folders** is optional and cannot be undone. The confirmation window lists each folder first. They can include settings and saves. Folders that another program or Windows may use are kept, and the app tells you when a folder is kept or could not be fully deleted. Registry settings are not cleaned, and some files elsewhere can remain.
- **Windows features** are turned off, not deleted. Some need a restart.
- **Services** are stopped and their startup type is set to Disabled. They are not deleted.

## What can go wrong

- Loss of app data, settings or saved files.
- Other apps that depend on what you removed stop working.
- Windows features stop working, such as search, the Store, widgets, printing to PDF or Xbox sign-in.
- Windows updates fail, or reinstall what you removed.
- Your device manufacturer's tools for drivers, hotkeys, fans or warranty support stop working.
- In the worst case, Windows needs repair or a clean installation.

## Limits of restore points

- Restore points may not bring back Store apps, and do not restore your personal files.
- Windows can delete old restore points when it needs space.
- Creating one turns on System Protection for the system drive if it is off. This uses disk space.
- If a restore point fails, the app asks before continuing without one.

## Good practice

- Back up important files first, and keep Windows installation media available.
- Keep the restore point option on.
- Remove a few items at a time, and restart and check Windows before you continue.
- Do not remove what you do not recognize.
- Do not use this on a work or school device without permission.

## Undoing changes

- **Store apps**: reinstall from the Microsoft Store. If the Store itself is gone, run `wsreset -i` as an administrator, or use a restore point.
- **Programs**: reinstall from the publisher.
- **Windows features**: open "Turn Windows features on or off".
- **Services**: set the startup type in `services.msc`. The previous value is in the log.
- **Anything else**: use System Restore, or Settings > System > Recovery.
