<#
.SYNOPSIS
    Lightweight installer for HP Button Remap
.DESCRIPTION
    Installs the tray utility to %LOCALAPPDATA% and registers startup.
    Uses framework-dependent single-file publish to keep package size small.
#>

$ErrorActionPreference = "Stop"

Write-Host "=== HP Button Remap Lightweight Installer ===" -ForegroundColor Cyan
Write-Host ""

$scriptDir = $PSScriptRoot
$projectPath = Join-Path $scriptDir "HPButtonRemap\HPButtonRemap.csproj"
$stagingDir = Join-Path $scriptDir ".publish-temp"
$appExe = Join-Path $scriptDir "HPButtonRemap.exe"
$configJson = Join-Path $scriptDir "config.json"

$installDir = Join-Path $env:LOCALAPPDATA "HPButtonRemap"
$startupFolder = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupFolder "HP Button Remap.lnk"

if (-not (Test-Path $appExe)) {
    if (-not (Test-Path $projectPath)) {
        throw "HPButtonRemap.exe not found and project file not present: $projectPath"
    }

    Write-Host "[INFO] Building lightweight single-file executable..." -ForegroundColor Cyan
    if (Test-Path $stagingDir) {
        Remove-Item $stagingDir -Recurse -Force
    }

    dotnet publish $projectPath `
        --configuration Release `
        --runtime win-x64 `
        --self-contained false `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=true `
        --output $stagingDir

    Copy-Item (Join-Path $stagingDir "HPButtonRemap.exe") -Destination $scriptDir -Force
    Remove-Item $stagingDir -Recurse -Force
}

Write-Host "[INFO] Installing to: $installDir" -ForegroundColor Cyan
New-Item -Path $installDir -ItemType Directory -Force | Out-Null

Write-Host "[INFO] Stopping existing instance (if running)..." -ForegroundColor Cyan
Get-Process -Name "HPButtonRemap" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Write-Host "[INFO] Copying files..." -ForegroundColor Cyan
Copy-Item $appExe -Destination (Join-Path $installDir "HPButtonRemap.exe") -Force
Copy-Item (Join-Path $scriptDir "Uninstall.ps1") -Destination (Join-Path $installDir "Uninstall.ps1") -Force

$destConfig = Join-Path $installDir "config.json"
if ((Test-Path $configJson) -and (-not (Test-Path $destConfig))) {
    Copy-Item $configJson -Destination $destConfig -Force
    Write-Host "  - config.json created" -ForegroundColor Gray
} elseif (Test-Path $destConfig) {
    Write-Host "  - config.json preserved" -ForegroundColor Gray
}

Write-Host "[INFO] Registering startup shortcut..." -ForegroundColor Cyan
$wsh = New-Object -ComObject WScript.Shell
$shortcut = $wsh.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $installDir "HPButtonRemap.exe"
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "HP Button Remap"
$shortcut.Save()

Write-Host "[INFO] Starting tray app..." -ForegroundColor Cyan
Start-Process (Join-Path $installDir "HPButtonRemap.exe") -WorkingDirectory $installDir

Write-Host ""
Write-Host "=== Installation Complete ===" -ForegroundColor Green
Write-Host "Installed files: $installDir" -ForegroundColor Gray
Write-Host "To uninstall: run $installDir\Uninstall.ps1" -ForegroundColor Yellow
Write-Host ""
