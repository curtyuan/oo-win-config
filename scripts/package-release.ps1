param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "dist/oo-win-config",
    [string]$ZipPath = "dist/oo-win-config.zip"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src/OoWinConfig/OoWinConfig.csproj"
$publishDirectory = Join-Path $repoRoot $OutputDirectory
$zipFullPath = Join-Path $repoRoot $ZipPath

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

dotnet publish $projectPath -c $Configuration -o $publishDirectory

$userDataDirectories = @(
    "data/backups",
    "data/trash",
    "data/settings",
    "data/debug"
)

foreach ($relativePath in $userDataDirectories) {
    $path = Join-Path $publishDirectory $relativePath
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

if (Test-Path -LiteralPath $zipFullPath) {
    Remove-Item -LiteralPath $zipFullPath -Force
}

Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $zipFullPath -Force

"Release package written to: $zipFullPath"
