# Builds installer\Capri-Mod-Linux-<version>.sh: one file that installs the modded Capri on Linux (Fedora) under
# Wine. Run from anywhere: powershell -ExecutionPolicy Bypass -File installer\build-linux.ps1
# On Linux, run the result with: bash Capri-Mod-Linux-<version>.sh
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$repo = Split-Path $here
$version = ([regex]'ModVersion = "([^"]+)"').Match((Get-Content "$repo\src\CapriDesktop\Mainframe.cs" -Raw)).Groups[1].Value
Write-Host "Capri mod $version (Linux)"

dotnet publish "$repo\src\Capri.exe.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o "$here\app" -v q -nologo
if ($LASTEXITCODE) { throw 'Capri build failed' }

# the payload: her exe, a menu icon and the uninstaller, packed as a .tar.gz after the script
$stage = "$here\linux-stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory $stage | Out-Null
Copy-Item "$here\app\Capri.exe" $stage
# the menu icon: the biggest picture inside app.ico (big ones are stored as PNG already)
$ico = [IO.File]::ReadAllBytes("$repo\src\app.ico")
$best = -1; $bestSize = 0
for ($i = 0; $i -lt [BitConverter]::ToUInt16($ico, 4); $i++) {
	$size = $ico[6 + 16 * $i]; if ($size -eq 0) { $size = 256 }
	if ($size -gt $bestSize) { $bestSize = $size; $best = $i }
}
$length = [BitConverter]::ToInt32($ico, 6 + 16 * $best + 8)
$offset = [BitConverter]::ToInt32($ico, 6 + 16 * $best + 12)
if ($ico[$offset] -eq 0x89 -and $ico[$offset + 1] -eq 0x50) {
	$png = New-Object byte[] $length
	[Array]::Copy($ico, $offset, $png, 0, $length)
	[IO.File]::WriteAllBytes("$stage\capri.png", $png)
} else {
	Add-Type -AssemblyName System.Drawing
	$icon = New-Object System.Drawing.Icon "$repo\src\app.ico", $bestSize, $bestSize
	$icon.ToBitmap().Save("$stage\capri.png", [System.Drawing.Imaging.ImageFormat]::Png)
	$icon.Dispose()
}
$lf = { param($path) (Get-Content $path -Raw) -replace "`r`n", "`n" }
$utf8 = New-Object System.Text.UTF8Encoding $false
[IO.File]::WriteAllText("$stage\uninstall.sh", (& $lf "$here\linux\uninstall.sh"), $utf8)
$payload = "$here\linux-payload.tar.gz"
& "$env:SystemRoot\System32\tar.exe" -czf $payload -C $stage .
if ($LASTEXITCODE) { throw 'Packing failed' }

$header = (& $lf "$here\linux\install.sh").Replace('@VERSION@', $version).TrimEnd("`n") + "`n"
$final = "$here\Capri-Mod-Linux-$version.sh"
$out = [IO.File]::Create($final)
try {
	$bytes = $utf8.GetBytes($header)
	$out.Write($bytes, 0, $bytes.Length)
	$in = [IO.File]::OpenRead($payload)
	try { $in.CopyTo($out) } finally { $in.Dispose() }
} finally { $out.Dispose() }
Remove-Item $stage -Recurse -Force
Remove-Item $payload -Force
Write-Host "Pronto: $final"
