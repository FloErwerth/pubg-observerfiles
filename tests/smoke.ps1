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

# Fill only missing team IDs; existing rows and image bytes must survive unchanged.
foreach ($i in @(0, 1, 2)) {
    $filled = Join-Path $root "filled-$i\Observer"
    [PubgObserver.Installer]::InstallPack($i, $filled, $true) | Out-Null
    $rows = @(Import-Csv -LiteralPath (Join-Path $filled 'TeamInfo.csv'))
    if (($rows.TeamNumber | ForEach-Object { [int]$_ } | Sort-Object) -join ',' -ne (1..100 -join ',')) { throw 'Auffuellen ergibt keine eindeutigen Teams 1-100.' }
    $original = Join-Path $root "embedded-$i\Observer"
    $originalRows = @(Import-Csv -LiteralPath (Join-Path $original 'TeamInfo.csv'))
    foreach ($originalRow in $originalRows) {
        $updated = $rows | Where-Object TeamNumber -eq $originalRow.TeamNumber
        if (($originalRow | ConvertTo-Json -Compress) -ne ($updated | ConvertTo-Json -Compress)) { throw 'Vorhandene Zuordnung veraendert.' }
    }
    foreach ($image in (Get-ChildItem -LiteralPath (Join-Path $original 'TeamIcon') -File)) {
        if ((Get-FileHash -LiteralPath $image.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $filled ('TeamIcon\' + $image.Name))).Hash) { throw 'Vorhandenes Bild veraendert.' }
    }
    foreach ($row in $rows) {
        $icon = Join-Path $filled ('TeamIcon\' + $row.ImageFileName)
        if (-not (Test-Path -LiteralPath $icon)) { throw 'Ergaenztes Bild fehlt.' }
        if ($row.ImageFileName -like 'observer-emoji-*') {
            $emoji = Join-Path $root ('embedded-1\Observer\TeamIcon\' + $row.TeamNumber + '.png')
            if ((Get-FileHash -LiteralPath $icon).Hash -ne (Get-FileHash -LiteralPath $emoji).Hash) { throw 'Falsches Emoji zugeordnet.' }
        }
    }
    # Applying the option again to a complete result should be byte-preserving.
    $again = Join-Path $root "again-$i\Observer"
    [PubgObserver.Installer]::Install($filled, $again, $true) | Out-Null
    if ((Get-FileHash -LiteralPath (Join-Path $filled 'TeamInfo.csv')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $again 'TeamInfo.csv')).Hash) { throw 'Vollstaendige CSV unnoetig umgeschrieben.' }
}

# Custom column ordering, quoted commas/quotes, a gap inside the range, an extra
# team above 100, and an existing filename collision must all be preserved.
$custom = Join-Path $root 'custom'
New-Item -ItemType Directory -Path (Join-Path $custom 'TeamIcon') -Force | Out-Null
$customCsv = 'ImageFileName,TeamNumber,TeamTags,TeamName,Extra' + "`r`n" +
    'existing.png,1,ONE,"Alpha, ""One""",keep' + "`r`n" +
    'existing.png,3,THREE,Three,keep3' + "`r`n" +
    'existing.png,101,EXTRA,Extra,keep101' + "`r`n"
[IO.File]::WriteAllText((Join-Path $custom 'TeamInfo.csv'), $customCsv)
Copy-Item -LiteralPath (Join-Path $root 'embedded-2\Observer\TeamIcon\ITA.png') -Destination (Join-Path $custom 'TeamIcon\existing.png')
Set-Content -LiteralPath (Join-Path $custom 'TeamIcon\observer-emoji-2.png') -Value 'preserve-collision'
$customTarget = Join-Path $root 'custom-target\Observer'
[PubgObserver.Installer]::Install($custom, $customTarget, $true) | Out-Null
$rows = @(Import-Csv -LiteralPath (Join-Path $customTarget 'TeamInfo.csv'))
if ($rows.Count -ne 101 -or ($rows | Where-Object TeamNumber -eq '1').TeamName -ne 'Alpha, "One"') { throw 'Custom-CSV falsch verarbeitet.' }
if (($rows | Where-Object TeamNumber -eq '101').Extra -ne 'keep101') { throw 'Zusatzteam verloren.' }
if (($rows | Where-Object TeamNumber -eq '2').ImageFileName -ne 'observer-emoji-2-1.png') { throw 'Dateinamenkollision nicht behandelt.' }
if ((Get-Content -LiteralPath (Join-Path $customTarget 'TeamIcon\observer-emoji-2.png')) -ne 'preserve-collision') { throw 'Kollision ueberschrieben.' }
if ([IO.File]::ReadAllText((Join-Path $custom 'TeamInfo.csv')) -ne $customCsv) { throw 'Quell-CSV veraendert.' }

$before = (Get-FileHash -LiteralPath (Join-Path $customTarget 'TeamInfo.csv')).Hash
[IO.File]::AppendAllText((Join-Path $custom 'TeamInfo.csv'), "existing.png,1,DUP,Duplicate,bad`r`n")
$rejected = $false
try { [PubgObserver.Installer]::Install($custom, $customTarget, $true) } catch { $rejected = $true }
if (-not $rejected -or (Get-FileHash -LiteralPath (Join-Path $customTarget 'TeamInfo.csv')).Hash -ne $before) { throw 'Ungueltige CSV hat die Installation veraendert.' }

# Show invisibly to inspect actual visibility, including custom-folder changes.
$form = New-Object PubgObserver.MainForm
try {
    $form.ShowInTaskbar = $false
    $form.Opacity = 0
    $form.Show()
    if (-not $form.Controls['FillMissing'].Visible -or $form.Controls['FillMissing'].Checked) { throw 'Checkbox muss optional angeboten werden.' }
    $form.Controls['FillMissing'].Checked = $true
    $form.Controls['PackSelection'].SelectedIndex = 1
    if ($form.Controls['FillMissing'].Visible -or $form.Controls['FillMissing'].Checked) { throw 'Checkbox bei 100 Teams nicht ausgeblendet.' }
    $form.Controls['PackSelection'].SelectedIndex = 3
    $form.Controls['SourceFolder'].Text = Join-Path $root 'embedded-2\Observer'
    if (-not $form.Controls['FillMissing'].Visible) { throw 'Checkbox bei eigenem Paket fehlt.' }
    $form.Controls['SourceFolder'].Text = Join-Path $root 'filled-2\Observer'
    if ($form.Controls['FillMissing'].Visible) { throw 'Checkbox bei vollstaendigem eigenem Paket sichtbar.' }
} finally { $form.Dispose() }
Write-Output 'Emoji-Ergaenzung: alle Pakete, Luecken, bestehende Zuordnungen, Kollisionen, Custom-CSV, Fehlerfall und Checkbox erfolgreich geprueft.'

foreach ($case in @(@('de-DE', 'de'), @('de-CH', 'de'), @('en-US', 'en'), @('fr-FR', 'en'), @('ja-JP', 'en'))) {
    if ([PubgObserver.Language]::Detect([Globalization.CultureInfo]::GetCultureInfo($case[0])) -ne $case[1]) { throw 'Falscher Sprach-Fallback.' }
}
[PubgObserver.Language]::Select('system')
$form = New-Object PubgObserver.MainForm
try {
    if ($form.Controls['LanguageSelection'].SelectedIndex -ne 0 -or [PubgObserver.Language]::Current -ne [PubgObserver.Language]::SystemLanguage) { throw 'Systemsprache nicht Standard.' }
    $form.Controls['PackSelection'].SelectedIndex = 2
    $form.Controls['FillMissing'].Checked = $true
    $form.Controls['LanguageSelection'].SelectedIndex = 2
    if (-not $form.Controls['FillMissing'].Text.Contains([string][char]0x00FC)) { throw 'Deutscher Umlaut fehlt.' }
    if ([PubgObserver.Language]::Current -ne 'de') { throw 'Deutsch nicht ausgewaehlt.' }
    $form.Controls['LanguageSelection'].SelectedIndex = 1
    if ($form.Controls['FillMissing'].Text -ne 'Fill missing assignments with emojis') { throw 'Englische Uebersetzung fehlt.' }
    if ($form.Controls['PackSelection'].SelectedIndex -ne 2 -or -not $form.Controls['FillMissing'].Checked) { throw 'Sprachwechsel verliert die Auswahl.' }
    if ($form.Controls['Donate'].Tag -ne 'https://buymeacoffee.com/forli69' -or $form.Controls['Donate'].AccessibleName -ne 'Buy me a coffee' -or $null -eq $form.Controls['Donate'].BackgroundImage) { throw 'Buy-Me-a-Coffee-Button falsch.' }
    $form.Controls['LanguageSelection'].SelectedIndex = 0
    if ([PubgObserver.Language]::Current -ne [PubgObserver.Language]::SystemLanguage) { throw 'Rueckkehr zur Systemsprache fehlgeschlagen.' }
} finally { $form.Dispose() }
Write-Output 'Deutsch, Englisch, System-Fallback, Umlaute, Auswahl-Erhalt und Buy-Me-a-Coffee-Button erfolgreich geprueft.'
