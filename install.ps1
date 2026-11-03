# Installs or uninstalls DebloatManager from the terminal.
param(
    [switch]$AcceptEula,
    [switch]$DownloadOnly,
    [switch]$Uninstall,
    [string]$Version = 'latest',
    [string]$BaseUrl
)

function Get-ReleaseBaseUrl {
    param([string]$Version)
    $repository = 'https://github.com/kwhyu/debloat-remover/releases'
    if ($Version -eq 'latest') { return "$repository/latest/download" }
    return "$repository/download/v$($Version.TrimStart('v'))"
}

function Confirm-Eula {
    Write-Host ''
    Write-Host 'DebloatManager permanently removes software and changes Windows settings.'
    Write-Host 'Removed apps can lose their data. Windows features or other apps can stop working.'
    Write-Host 'The software comes with no warranty, and you are responsible for what you remove.'
    Write-Host ''
    Write-Host 'EULA:    https://github.com/kwhyu/debloat-remover/blob/main/EULA.md'
    Write-Host 'Risks:   https://github.com/kwhyu/debloat-remover/blob/main/RISKS.md'
    Write-Host 'Privacy: https://github.com/kwhyu/debloat-remover/blob/main/PRIVACY.md'
    Write-Host ''
    return (Read-Host 'Type ACCEPT to accept the EULA and continue') -ceq 'ACCEPT'
}

function Invoke-Msiexec {
    param([string]$Arguments)
    $process = Start-Process -FilePath "$env:SystemRoot\System32\msiexec.exe" -ArgumentList $Arguments -Verb RunAs -Wait -PassThru
    return $process.ExitCode
}

function Get-InstalledProductCode {
    $paths = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
    )
    $entry = Get-ItemProperty -Path $paths -ErrorAction SilentlyContinue |
        Where-Object { $_.DisplayName -eq 'DebloatManager' -and $_.WindowsInstaller -eq 1 } |
        Select-Object -First 1
    if ($entry) { return $entry.PSChildName }
    return $null
}

function Write-MsiResult {
    param([int]$ExitCode, [string]$Done)
    switch ($ExitCode) {
        0 { Write-Host $Done }
        3010 { Write-Host "$Done Restart Windows to finish." }
        1602 { Write-Host 'Cancelled.' }
        1223 { Write-Host 'Cancelled at the administrator prompt.' }
        default { throw "Windows Installer failed with code $ExitCode." }
    }
}

function Invoke-DebloatManagerSetup {
    param([switch]$AcceptEula, [switch]$DownloadOnly, [switch]$Uninstall, [string]$Version = 'latest', [string]$BaseUrl)

    $ErrorActionPreference = 'Stop'
    $ProgressPreference = 'SilentlyContinue'

    if ($Uninstall) {
        $code = Get-InstalledProductCode
        if (-not $code) { Write-Host 'DebloatManager is not installed.'; return }
        Write-MsiResult -ExitCode (Invoke-Msiexec "/x $code /passive /norestart") -Done 'DebloatManager was uninstalled.'
        return
    }

    if (-not [Environment]::Is64BitOperatingSystem) { throw 'DebloatManager requires 64-bit Windows.' }

    if (-not $AcceptEula -and -not (Confirm-Eula)) {
        Write-Host 'The EULA was not accepted. Nothing was installed.'
        return
    }

    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    if (-not $BaseUrl) { $BaseUrl = Get-ReleaseBaseUrl -Version $Version }

    $folder = Join-Path ([IO.Path]::GetTempPath()) "DebloatManager-$([Guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $folder | Out-Null
    $msiPath = Join-Path $folder 'DebloatManagerSetup.msi'
    $hashPath = "$msiPath.sha256"
    $keepFiles = $false

    try {
        Write-Host 'Downloading the installer...'
        Invoke-WebRequest -Uri "$BaseUrl/DebloatManagerSetup.msi" -OutFile $msiPath -UseBasicParsing
        Invoke-WebRequest -Uri "$BaseUrl/DebloatManagerSetup.msi.sha256" -OutFile $hashPath -UseBasicParsing

        $expected = ((Get-Content -Path $hashPath -Raw).Trim() -split '\s+')[0]
        $actual = (Get-FileHash -Path $msiPath -Algorithm SHA256).Hash
        if ($expected -ine $actual) { throw 'Checksum mismatch. The download was rejected and nothing was installed.' }
        Write-Host 'Checksum verified.'

        if ($DownloadOnly) {
            $keepFiles = $true
            Write-Host "Saved to $msiPath"
            return
        }

        Write-Host 'Installing. Approve the administrator prompt to continue...'
        $done = 'DebloatManager was installed. Open it from the Start menu.'
        Write-MsiResult -ExitCode (Invoke-Msiexec "/i `"$msiPath`" /passive /norestart") -Done $done
    }
    finally {
        if (-not $keepFiles) { Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

Invoke-DebloatManagerSetup @PSBoundParameters
