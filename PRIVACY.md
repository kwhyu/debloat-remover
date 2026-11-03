# Privacy Policy

Effective 3 November 2026.

DebloatManager does not collect personal data and does not send anything to the author. It has no accounts, analytics, telemetry, ads or update checks, and the app makes no network requests of its own.

## What stays on your device

DebloatManager reads the list of installed apps, programs, Windows features and services on your device to show them to you. It also checks the digital signatures of program files using Windows, offline. That information is not sent anywhere.

It writes these files under `%LocalAppData%\DebloatManager`:

- `logs\`: what you scanned and changed, with timestamps. Logs can include program names, install paths and your Windows user name as part of a path. Logs older than 30 days are deleted automatically.
- `accepted-terms.txt`: the version of the terms you accepted.

Other administrators of the device can read these files. Uninstalling DebloatManager does not delete them. To remove them, delete the `%LocalAppData%\DebloatManager` folder.

## When something else uses the network

- **Terminal installer.** `install.ps1` downloads the installer and its checksum from GitHub. GitHub receives normal request data such as your IP address, under the [GitHub Privacy Statement](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement).
- **Third-party uninstallers.** When you remove a program, its publisher's uninstaller runs. It may open a web page or contact its publisher. DebloatManager does not control this.
- **Windows.** Windows itself may contact Microsoft during these operations.

## If you share logs

Log files stay private unless you share them. Check them for personal details such as your user name before you attach them to a GitHub issue. GitHub issues are public.

## Children

DebloatManager is a system administration tool and is not directed at children. It collects no data from anyone.

## Changes

Any change to this policy is published in this repository before the release it applies to.

## Contact

https://github.com/kwhyu/debloat-remover/issues
