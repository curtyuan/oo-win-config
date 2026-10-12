param(
    [string]$InstallDirectory = (Split-Path -Parent $PSScriptRoot),
    [string]$ReleaseUrl = "https://github.com/curtyuan/oo-win-config/releases/latest/download/oo-win-config.zip"
)

$ErrorActionPreference = "Stop"

$installPath = Resolve-Path $InstallDirectory
$downloadPath = Join-Path $env:TEMP "oo-win-config.zip"
$extractPath = Join-Path $env:TEMP "oo-win-config-update"

Invoke-WebRequest -Uri $ReleaseUrl -OutFile $downloadPath

if (Test-Path -LiteralPath $extractPath) {
    Remove-Item -LiteralPath $extractPath -Recurse -Force
}

Expand-Archive -Path $downloadPath -DestinationPath $extractPath -Force

$preservedDataDirectories = @(
    "backups",
    "trash",
    "settings",
    "debug"
)

Get-ChildItem -LiteralPath $extractPath -Force | ForEach-Object {
    if ($_.Name -eq "data") {
        $sourceProfiles = Join-Path $_.FullName "profiles"
        if (Test-Path -LiteralPath $sourceProfiles) {
            $targetProfiles = Join-Path $installPath "data/profiles"
            if (Test-Path -LiteralPath $targetProfiles) {
                Remove-Item -LiteralPath $targetProfiles -Recurse -Force
            }

            New-Item -ItemType Directory -Path (Split-Path -Parent $targetProfiles) -Force | Out-Null
            Copy-Item -LiteralPath $sourceProfiles -Destination $targetProfiles -Recurse -Force
        }

        foreach ($directory in $preservedDataDirectories) {
            $path = Join-Path $installPath "data/$directory"
            if (Test-Path -LiteralPath $path) {
                "Preserved: $path"
            }
        }

        return
    }

    $target = Join-Path $installPath $_.Name
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force
    }

    Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force
}

"Updated oo-win-config in: $installPath"
