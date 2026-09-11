param(
    [ValidateSet('Apply','Restore')][string]$Mode = 'Apply',
    [string]$ObserverPath = (Join-Path $env:LOCALAPPDATA 'TslGame\Saved\Observer')
)
$ErrorActionPreference = 'Stop'
$defaultObserver = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'TslGame\Saved\Observer'))
$ObserverPath = [IO.Path]::GetFullPath($ObserverPath)
if ($ObserverPath -eq $defaultObserver -and (Get-Process TslGame -ErrorAction SilentlyContinue)) {
    throw 'PUBG zuerst schliessen. Es wurden keine Dateien geaendert.'
}
$csv = Join-Path $ObserverPath 'TeamInfo.csv'
$hasher = [Security.Cryptography.SHA256]::Create()
try { $key = [BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($ObserverPath.TrimEnd('\').ToUpperInvariant()))).Replace('-','') }
finally { $hasher.Dispose() }
$stateDirectory = Join-Path $env:LOCALAPPDATA ('PUBG Observer Installer\' + $key + '\diagnostics')
New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
$backup = Join-Path $stateDirectory 'TeamInfo.before-team29-test.txt'
$expected = Join-Path $stateDirectory 'TeamInfo.team29-test.sha256'
# Migrate the exact two artifacts created by previous versions, preserving their contents.
foreach ($name in @('TeamInfo.before-team29-test.txt','TeamInfo.team29-test.sha256')) {
    $legacy = Join-Path $ObserverPath $name
    $destination = Join-Path $stateDirectory $name
    if (Test-Path -LiteralPath $legacy) {
        if (Test-Path -LiteralPath $destination) { throw 'Alte und neue Testsicherung vorhanden; keine Sicherung wird ueberschrieben.' }
        Move-Item -LiteralPath $legacy -Destination $destination
    }
}
if ($Mode -eq 'Restore') {
    if (-not (Test-Path -LiteralPath $backup) -or -not (Test-Path -LiteralPath $expected)) { throw 'Keine vollstaendige Testsicherung vorhanden.' }
    if ((Get-FileHash -LiteralPath $csv).Hash -ne ([IO.File]::ReadAllText($expected)).Trim()) { throw 'CSV wurde seit dem Test veraendert. Sicherung bleibt zur manuellen Wiederherstellung erhalten.' }
    Copy-Item -LiteralPath $backup -Destination $csv -Force
    Remove-Item -LiteralPath $backup,$expected
    Write-Output 'Urspruengliche CSV wiederhergestellt.'
    exit 0
}
if ((Test-Path -LiteralPath $backup) -or (Test-Path -LiteralPath $expected)) { throw 'Ein Test ist bereits vorbereitet. Zuerst mit -Mode Restore zuruecksetzen.' }
$content = [IO.File]::ReadAllText($csv)
$rows = @($content | ConvertFrom-Csv)
$team26 = @($rows | Where-Object TeamNumber -eq '26')
$team29 = @($rows | Where-Object TeamNumber -eq '29')
if ($team26.Count -ne 1 -or $team29.Count -ne 1) { throw 'Team 26/29 fehlt oder ist doppelt.' }
$pattern = '(?m)^(29,[^,\r\n]*,[^,\r\n]*,)([^,\r\n]+)(,[^\r\n]*)(\r?)$'
$match = [regex]::Match($content,$pattern)
if (-not $match.Success -or $match.Groups[2].Value -ne $team29[0].ImageFileName) { throw 'Unerwartetes CSV-Format; keine Aenderung.' }
$image26 = $team26[0].ImageFileName
if ($image26 -match '[\\/,$\r\n"]' -or -not (Test-Path -LiteralPath (Join-Path $ObserverPath ('TeamIcon\'+$image26)))) { throw 'Bild fuer Team 26 ist ungueltig oder fehlt.' }
if ($image26 -eq $team29[0].ImageFileName) { throw 'Beide Teams verwenden bereits dasselbe Bild.' }
$changed = $content.Substring(0,$match.Groups[2].Index) + $image26 + $content.Substring($match.Groups[2].Index+$match.Groups[2].Length)
Copy-Item -LiteralPath $csv -Destination $backup
try {
    [IO.File]::WriteAllText($csv,$changed,(New-Object Text.UTF8Encoding($false)))
    [IO.File]::WriteAllText($expected,(Get-FileHash -LiteralPath $csv).Hash)
} catch {
    Copy-Item -LiteralPath $backup -Destination $csv -Force
    throw
}
Write-Output 'Test aktiv: Team 29 verweist auf das gruene Emoji samt Nummer 26. Alle anderen Zuordnungen und Bilder sind unveraendert.'
Write-Output 'PUBG neu starten und dieselbe Stelle im Replay pruefen. Danach PUBG schliessen und dieses Skript mit -Mode Restore ausfuehren.'
