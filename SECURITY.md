# Security Policy

DebloatManager runs with administrator rights, so security problems matter.

## Reporting a vulnerability

Do not open a public issue. Use [private vulnerability reporting](https://github.com/kwhyu/debloat-remover/security/advisories/new) on GitHub and include the steps to reproduce, the affected version and the possible impact.

This is a volunteer project, so response times are not guaranteed. Reports are handled as quickly as possible.

## Supported versions

Only the latest release receives fixes.

## Verifying a download

Each release file has a SHA-256 checksum, and `install.ps1` checks it automatically. To check manually:

```powershell
Get-FileHash .\DebloatManagerSetup.msi -Algorithm SHA256
```

Compare the result with `DebloatManagerSetup.msi.sha256` from the same release. Releases are not code signed at this time. Download only from this repository.
