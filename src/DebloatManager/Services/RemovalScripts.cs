namespace DebloatManager.Services;

public static class RemovalScripts
{
    private const string StoreAppBody = """

        $ErrorActionPreference = 'Stop'
        $store = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore'
        $errors = New-Object System.Collections.ArrayList
        $endOfLifeKeys = New-Object System.Collections.ArrayList
        $families = New-Object System.Collections.ArrayList

        function Get-TargetPackages {
            @(Get-AppxPackage -AllUsers -Name $name | Where-Object { $_.Name -eq $name })
        }

        function Get-TargetProvisioned {
            @(Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -eq $name })
        }

        function Test-PackageActive($package) {
            $users = @($package.PackageUserInformation)
            if ($users.Count -eq 0) { return $true }
            foreach ($user in $users) {
                if (@('Staged', 'NotInstalled') -notcontains "$($user.InstallState)") { return $true }
            }
            return $false
        }

        function Get-PackageSids($package) {
            @($package.PackageUserInformation | ForEach-Object { "$($_.UserSecurityId.Sid)" } | Where-Object { $_ })
        }

        function Stop-PackageProcesses($package) {
            $location = "$($package.InstallLocation)".TrimEnd('\')
            if ($location.Length -le 3) { return }
            foreach ($process in Get-Process) {
                $path = $process.Path
                if ($path -and $path.StartsWith("$location\", [StringComparison]::OrdinalIgnoreCase)) {
                    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                }
            }
        }

        function Remove-TargetPackage($package) {
            $fullName = $package.PackageFullName
            try { Remove-AppxPackage -Package $fullName -AllUsers; return } catch { }

            # Windows refuses inbox packages unless they are marked end-of-life for each user.
            foreach ($sid in Get-PackageSids $package) {
                $key = "$store\EndOfLife\$sid\$fullName"
                New-Item -Path $key -Force | Out-Null
                [void]$endOfLifeKeys.Add($key)
            }

            try { Remove-AppxPackage -Package $fullName -AllUsers; return } catch { }

            foreach ($sid in Get-PackageSids $package) {
                try { Remove-AppxPackage -Package $fullName -User $sid }
                catch { [void]$errors.Add("$fullName`: $($_.Exception.Message)") }
            }
        }

        try {
            $packages = Get-TargetPackages
            foreach ($package in $packages) {
                [void]$families.Add($package.PackageFamilyName)
                Stop-PackageProcesses $package
            }

            foreach ($provisioned in Get-TargetProvisioned) {
                try { Remove-AppxProvisionedPackage -Online -PackageName $provisioned.PackageName | Out-Null }
                catch { [void]$errors.Add("Provisioned: $($_.Exception.Message)") }
            }

            foreach ($package in $packages) { Remove-TargetPackage $package }
        }
        catch { [void]$errors.Add($_.Exception.Message) }
        finally {
            foreach ($key in $endOfLifeKeys) { Remove-Item -Path $key -Force -ErrorAction SilentlyContinue }
        }

        $remaining = @(Get-TargetPackages | Where-Object { Test-PackageActive $_ })
        $stillProvisioned = @(Get-TargetProvisioned)
        $removed = ($remaining.Count -eq 0 -and $stillProvisioned.Count -eq 0)

        # The Deprovisioned marker stops Windows feature updates from reinstalling the app.
        if ($removed) {
            foreach ($family in ($families | Select-Object -Unique)) {
                New-Item -Path "$store\Deprovisioned\$family" -Force -ErrorAction SilentlyContinue | Out-Null
            }
        }

        @{ removed = $removed; remaining = $remaining.Count; provisioned = $stillProvisioned.Count; errors = @($errors) } | ConvertTo-Json -Compress
        """;

    private const string FeatureBody = """

        $ErrorActionPreference = 'Stop'
        $result = Disable-WindowsOptionalFeature -Online -FeatureName $name -NoRestart
        $state = "$((Get-WindowsOptionalFeature -Online -FeatureName $name).State)"
        @{ state = $state; restartNeeded = [bool]$result.RestartNeeded } | ConvertTo-Json -Compress
        """;

    private const string ServiceBody = """

        $ErrorActionPreference = 'Stop'
        $previous = "$((Get-Service -Name $name).StartType)"
        Stop-Service -Name $name -Force -ErrorAction SilentlyContinue
        try { Set-Service -Name $name -StartupType Disabled }
        catch { Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\$name" -Name Start -Value 4 -Type DWord }
        $service = Get-Service -Name $name
        @{ previous = $previous; startType = "$($service.StartType)"; status = "$($service.Status)" } | ConvertTo-Json -Compress
        """;

    public static string StoreApp(string packageName) => Named(packageName) + StoreAppBody;

    public static string Feature(string featureName) => Named(featureName) + FeatureBody;

    public static string Service(string serviceName) => Named(serviceName) + ServiceBody;

    private static string Named(string value) => $"$name = {PowerShellRunner.Quote(value)}";
}
