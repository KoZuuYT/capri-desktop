# Builds installer\Capri-Mod-Setup-<version>.exe: one file that installs the modded Capri on another PC
# (no .NET needed there). Run from anywhere: powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$repo = Split-Path $here
$version = ([regex]'ModVersion = "([^"]+)"').Match((Get-Content "$repo\src\CapriDesktop\Mainframe.cs" -Raw)).Groups[1].Value
Write-Host "Capri mod $version"

dotnet publish "$repo\src\Capri.exe.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o "$here\app" -v q -nologo
if ($LASTEXITCODE) { throw 'Capri build failed' }

dotnet build "$here\Setup\Setup.csproj" -c Release -p:Uninstaller=true -p:Version=$version -o "$here\uninstall" -v q -nologo
if ($LASTEXITCODE) { throw 'Uninstaller build failed' }

dotnet build "$here\Setup\Setup.csproj" -c Release -p:Version=$version -o "$here\out" -v q -nologo --no-incremental
if ($LASTEXITCODE) { throw 'Setup build failed' }

$final = "$here\Capri-Mod-Setup-$version.exe"
Copy-Item "$here\out\Capri-Mod-Setup.exe" $final -Force
Write-Host "Pronto: $final"
