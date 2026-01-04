<#
.SYNOPSIS
    Simple Installer for HP Button Remap - Using Startup Folder
.DESCRIPTION
    Creates a shortcut in the Startup folder to run the application at logon.
    This method doesn't require administrator privileges.
#>

$ErrorActionPreference = "Stop"

Write-Host "=== HP Button Remap Installer (Startup Folder) ===" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$scriptDir = $PSScriptRoot

# Check for prebuilt executable first (from release download)
$prebuiltPath = Join-Path $scriptDir "HPButtonRemap.exe"
$builtPath = Join-Path $scriptDir "HPButtonRemap\bin\Release\net8.0-windows\HPButtonRemap.exe"
$configFile = Join-Path $scriptDir "config.json"

# Determine which executable to use
$appPath = $null
if (Test-Path $prebuiltPath) {
    Write-Host "[INFO] Using prebuilt executable" -ForegroundColor Cyan
    $appPath = $prebuiltPath
} elseif (Test-Path $builtPath) {
    Write-Host "[INFO] Using locally built executable" -ForegroundColor Cyan
    $appPath = $builtPath
} else {
    # Try to build from source
    Write-Host "[INFO] Executable not found. Building from source..." -ForegroundColor Yellow
    $projectPath = Join-Path $scriptDir "HPButtonRemap"
    
    if (Test-Path $projectPath) {
        Push-Location $projectPath
        try {
            dotnet build --configuration Release
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed"
            }
            $appPath = $builtPath
        } finally {
            Pop-Location
        }
    } else {
        Write-Host "[ERROR] Cannot find prebuilt executable or source code" -ForegroundColor Red
        Write-Host "Please download the release package from GitHub or clone the repository" -ForegroundColor Red
        exit 1
    }
}

# Validate files exist
if (-not (Test-Path $appPath)) {
    Write-Host "[ERROR] Application not found: $appPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $configFile)) {
    Write-Host "[ERROR] Configuration file not found: $configFile" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Found required files" -ForegroundColor Green

# Get startup folder path
$startupFolder = [Environment]::GetFolderPath('Startup')

# Create a VBS script to run the app hidden (no console window)
$vbsScriptPath = Join-Path $scriptDir "HPButtonRemap-Hidden.vbs"
$vbsContent = @"
Set objShell = CreateObject("WScript.Shell")
objShell.Run """$appPath""", 0, False
"@
Set-Content -Path $vbsScriptPath -Value $vbsContent

# Create shortcut in startup folder
$shortcutPath = Join-Path $startupFolder "HP Button Remap.lnk"
$WScriptShell = New-Object -ComObject WScript.Shell
$shortcut = $WScriptShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = "wscript.exe"
$shortcut.Arguments = "`"$vbsScriptPath`""
$shortcut.WorkingDirectory = $scriptDir
$shortcut.Description = "HP Button Remap - Background Application"
$shortcut.Save()

Write-Host ""
Write-Host "[SUCCESS] Installation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Method: Startup folder shortcut" -ForegroundColor Cyan
Write-Host "Location: $shortcutPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "The application will start automatically at next logon." -ForegroundColor Cyan
Write-Host "To start it now, run: Start-Process -FilePath wscript.exe -ArgumentList `"$vbsScriptPath`"" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration file: $configFile" -ForegroundColor Yellow
Write-Host "Edit this file to customize your button actions." -ForegroundColor Yellow
Write-Host ""
Write-Host "To uninstall: Delete the shortcut from your Startup folder" -ForegroundColor Yellow
Write-Host "  or run Uninstall-Startup.ps1" -ForegroundColor Yellow
Write-Host ""
