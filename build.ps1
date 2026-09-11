param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x Compiler fehlt.' }
$output = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $output | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
$resources = @("/resource:$PSScriptRoot\assets\buy-me-a-coffee.png,Brand.BuyMeACoffee.png")
foreach ($pack in @('flags-with-numbers', 'emojis', 'flags')) {
    $folder = Join-Path $PSScriptRoot "packages\$pack\Observer"
    if (-not (Test-Path -LiteralPath (Join-Path $folder 'TeamInfo.csv'))) { throw "Paket fehlt: $pack" }
    $archive = Join-Path $output "$pack.zip"
    if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive }
    [IO.Compression.ZipFile]::CreateFromDirectory($folder, $archive)
    $resources += "/resource:$archive,Packs.$pack.zip"
}
& $compiler /nologo /codepage:65001 /target:winexe /optimize+ /reference:Microsoft.VisualBasic.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "/win32manifest:$PSScriptRoot\src\app.manifest" $resources "/out:$output\PUBG-Observer-Installer.exe" (Join-Path $PSScriptRoot 'src\Installer.cs') (Join-Path $PSScriptRoot 'src\TeamCsv.cs') (Join-Path $PSScriptRoot 'src\Language.cs') (Join-Path $PSScriptRoot 'src\TeamNumbers.cs')
if ($LASTEXITCODE -ne 0) { throw 'Build fehlgeschlagen.' }
Write-Output "Erstellt: $output\PUBG-Observer-Installer.exe"
$hash = (Get-FileHash -LiteralPath (Join-Path $output 'PUBG-Observer-Installer.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $output 'SHA256SUMS.txt'), "$hash  PUBG-Observer-Installer.exe`n", [Text.Encoding]::ASCII)
