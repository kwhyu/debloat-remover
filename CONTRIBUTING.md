# Contributing

Contributions are welcome. This tool runs with administrator rights and changes people's PCs, so every change is reviewed carefully before it is merged.

By contributing, you agree that your contribution is licensed under the [MIT License](LICENSE). You keep the copyright to your own work. Be respectful to everyone involved.

## Ways to help

- **Catalog entries:** add apps that are missing, or correct a risk level. This is the most useful contribution, because Windows changes often.
- **Bug reports:** open an issue with your Windows version, what you selected, and the message you saw. Check logs for personal details such as your user name before you attach them.
- **Testing:** report results from Windows versions you tested in a virtual machine.
- **Fixes and documentation:** open a pull request. For a large change or a new feature, open an issue first so we can agree on the approach.

Report security problems privately, as described in [SECURITY.md](SECURITY.md).

## What will not be accepted

- Telemetry, ads, or bundled software.
- Downloading or running code from the internet inside the app.
- Removing Windows Security, or anything that weakens Windows security by default.
- Broad catalog patterns such as `*Wallet*`, because they can match someone's unrelated program.
- Changes that hide or soften risk warnings.

## Branches

```text
your branch  ->  snapshot  ->  main
```

- `main` holds released code. It only accepts pull requests from `snapshot`.
- `snapshot` collects changes that are being prepared for release. It only accepts pull requests.
- Your work goes on a new branch named `feature/short-name` for new work or `bugfix/short-name` for fixes.

Direct pushes to `main` and `snapshot` are blocked.

## Pull requests

1. Fork the repository and create a `feature/` or `bugfix/` branch from `snapshot`.
2. Keep the change small and focused on one thing.
3. Run `dotnet test`, and make sure it passes.
4. For any change to removal behavior, complete the virtual machine checks below.
5. Open a pull request into `snapshot` that explains what changed, why, and how you tested it.

## Code style

- Keep the code short and clear. Names should explain what code does.
- Comments are in English and at most one line. Longer explanations belong in the pull request.
- The interface stays simple and gray, with no accent colors or gradients.

## Tests

`dotnet test` is safe on your own PC. It needs no administrator rights and only touches a throwaway per-user registry entry and folder that each test creates and deletes. System scripts are checked for valid syntax but never executed.

## Testing real removal

**Use a disposable virtual machine, never your own PC.** Take a snapshot first.

Run the app as administrator in the VM and check each path:

1. **Store app:** remove Solitaire or Weather. It must disappear from Start, `Get-AppxPackage -AllUsers` and `Get-AppxProvisionedPackage -Online`.
2. **New user:** create a new Windows user and confirm the app is not there.
3. **MSI program:** install any small MSI, remove it, and confirm it is gone from Settings > Apps.
4. **Program with its own uninstaller:** remove something like 7-Zip with the delete option off, and confirm its folder stays. Install it again and remove it with the delete option on, and confirm the listed folders are gone. Then cancel an interactive uninstaller for another program and confirm the app reports Failed.
5. **Locked leftover file:** keep a file in the program folder open in another app while deleting. The status must be Files remain, and the message must name the folder.
6. **Non-removable package:** remove `Microsoft.Windows.NarratorQuickStart` and restart. Windows must still sign in and Start must still open.
7. **Feature and service:** turn off Telnet Client and disable the Fax service, and confirm the state in Windows.
8. **Restore point:** run it twice in a row and confirm two new entries in `rstrui`.
9. **Rescan:** every removed item must stay gone.

Restore the snapshot afterwards. Record the Windows version you tested in your pull request.

## Catalog changes

Add entries to `src/DebloatManager/Services/BloatwareCatalog.cs` using exact package names where possible. Mark an entry Safe only if it is preinstalled and nothing depends on it. If you are unsure, use Caution.

## Publishing a release

This section is for the maintainer.

1. Update `<Version>` in `src/DebloatManager/DebloatManager.csproj` and add a `CHANGELOG.md` entry.
2. Complete the virtual machine checks above on the `snapshot` branch.
3. Open a pull request from `snapshot` into `main` and merge it.
4. Run `installer\build-installer.ps1` from `main`.
5. Create a GitHub release tagged `v<version>` and attach these files from `publish\`, using exactly these names:
   - `DebloatManagerSetup.msi`
   - `DebloatManagerSetup.msi.sha256`
   - `DebloatManager.exe`
   - `DebloatManager.exe.sha256`

`install.ps1` downloads the first two files from the latest release.
