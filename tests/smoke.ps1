param([string]$InstallerPath = (Join-Path $PSScriptRoot '..\dist\PUBG-Observer-Installer.exe'))
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -eq 'Core') {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -InstallerPath $InstallerPath
    if ($LASTEXITCODE -ne 0) { throw 'Smoke-Pruefungen fehlgeschlagen.' }
    exit 0
}
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.IO.Compression
[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($InstallerPath)) | Out-Null
$root = Join-Path ([IO.Path]::GetTempPath()) ('pubg-observer-test-' + [Guid]::NewGuid().ToString('N'))
if (([PubgObserver.Installer]::PackIds -join ',') -ne 'flags-with-numbers,emojis,flags') { throw 'Falsche Paketauswahl.' }
$source = Join-Path $root 'pack'
$target = Join-Path $root 'Saved\Observer'
New-Item -ItemType Directory -Path (Join-Path $source 'TeamIcon') -Force | Out-Null
Set-Content -LiteralPath (Join-Path $source 'TeamInfo.csv') -Value 'test-package'
Set-Content -LiteralPath (Join-Path $source 'TeamIcon\1.png') -Value 'test-icon'
$backup = [PubgObserver.Installer]::Install($source, $target)
if ($backup) { throw 'Erstinstallation darf kein Backup erzeugen.' }
if (-not (Test-Path -LiteralPath (Join-Path $target 'TeamIcon\1.png'))) { throw 'Datei fehlt.' }
Set-Content -LiteralPath (Join-Path $target 'old.txt') -Value 'preserve-me'
$backup = [PubgObserver.Installer]::Install($source, $target)
if ((Get-Content -LiteralPath (Join-Path $backup 'old.txt')) -ne 'preserve-me') { throw 'Backup fehlerhaft.' }
if (Test-Path -LiteralPath (Join-Path $target 'old.txt')) { throw 'Veraltete Datei im Ziel.' }
$rejected = $false
try { [PubgObserver.Installer]::Install($target, $target) } catch { $rejected = $true }
if (-not $rejected) { throw 'Ueberlappende Pfade akzeptiert.' }
$rejected = $false
try { [PubgObserver.Installer]::Install($root, $target) } catch { $rejected = $true }
if (-not $rejected) { throw 'Ueberlappende Elternpfade akzeptiert.' }
$invalid = Join-Path $root 'invalid'
New-Item -ItemType Directory -Path $invalid | Out-Null
$rejected = $false
try { [PubgObserver.Installer]::Install($invalid, $target) } catch { $rejected = $true }
if (-not $rejected) { throw 'Ungueltiges Paket akzeptiert.' }
if ((Get-Content -LiteralPath (Join-Path $target 'TeamInfo.csv')) -ne 'test-package') { throw 'Ziel nach Ablehnung veraendert.' }
Write-Output "Alle 6 Smoke-Pruefungen erfolgreich. Testdaten: $root"
for ($i = 0; $i -lt 3; $i++) {
    $packTarget = Join-Path $root "embedded-$i\Observer"
    [PubgObserver.Installer]::InstallPack($i, $packTarget) | Out-Null
    $packSource = Join-Path $PSScriptRoot ("..\packages\" + [PubgObserver.Installer]::PackIds[$i] + '\Observer')
    $expected = @(Get-ChildItem -LiteralPath $packSource -Recurse -File)
    $actual = @(Get-ChildItem -LiteralPath $packTarget -Recurse -File)
    if ($actual.Count -ne $expected.Count) { throw 'Falsche Dateianzahl im eingebetteten Paket.' }
    foreach ($file in $expected) {
        $relative = $file.FullName.Substring((Get-Item -LiteralPath $packSource).FullName.Length + 1)
        if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $packTarget $relative)).Hash) { throw "Datei weicht ab: $relative" }
    }
    $csv = Get-ChildItem -LiteralPath $packTarget -Filter '*.csv'
    $rows = @(Import-Csv -LiteralPath $csv.FullName)
    $expectedTeams = @(50, 100, 25)[$i]
    if ((($rows.TeamNumber | ForEach-Object { [int]$_ } | Sort-Object) -join ',') -ne ((1..$expectedTeams) -join ',')) { throw 'Teamnummern fehlen oder sind doppelt.' }
    foreach ($row in $rows) {
        if (-not (Test-Path -LiteralPath (Join-Path $packTarget ('TeamIcon\' + $row.ImageFileName)))) { throw "Paket verweist auf fehlendes Icon: $($row.ImageFileName)" }
        $img = [Drawing.Image]::FromFile((Join-Path $packTarget ('TeamIcon\' + $row.ImageFileName)))
        try { if ($img.Width -lt 1 -or $img.Height -lt 1) { throw 'Ungueltiges Bild.' } } finally { $img.Dispose() }
    }
}
$form = New-Object PubgObserver.MainForm
if ($form.Controls['PackSelection'].SelectedIndex -ne 0) { throw 'Standardpaket falsch.' }
$form.Controls['PackSelection'].SelectedIndex = 2
if ($form.Controls['PackCoverage'].Text -notlike '*1-25*') { throw 'Abdeckung fuer Flaggen ohne Nummern fehlt.' }
$form.Dispose()
Write-Output 'Alle drei eingebetteten Pakete bytegenau geprueft; Flaggen mit Nummern sind vorausgewaehlt.'
