param([string]$InstallerPath = (Join-Path $PSScriptRoot '..\dist\PUBG-Observer-Installer.exe'))
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -eq 'Core') {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -InstallerPath $InstallerPath
    if ($LASTEXITCODE -ne 0) { throw 'Observer-Kompatibilitaetspruefung fehlgeschlagen.' }
    exit 0
}
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.IO.Compression
[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($InstallerPath)) | Out-Null
$root = Join-Path $env:TEMP ('pubg-observer-compatibility-' + [Guid]::NewGuid().ToString('N'))
# Validate raw output, not just a tolerant CSV parser: every shipped row must
# have five non-empty fields, an RGBA color and a decodable, visible icon.
# This checks the package contract; it does not emulate PUBG's closed parser.
foreach ($pack in @(0, 1)) {
    foreach ($numbers in @($false, $true)) {
        $folder = Join-Path $root ($pack.ToString() + '-' + $numbers)
        [PubgObserver.Installer]::InstallPack($pack, $folder, $true, $numbers) | Out-Null
        $lines = [IO.File]::ReadAllLines((Join-Path $folder 'TeamInfo.csv'))
        if ($lines.Count -ne 101) { throw 'Teams 1-100 sind nicht vollstaendig.' }
        $seen = @{}
        foreach ($line in $lines | Select-Object -Skip 1) {
            $fields = $line.Split(',')
            if ($fields.Count -ne 5 -or $fields[0] -notmatch '^\d+$') { throw "Ungueltige CSV-Zeile: $line" }
            $team = [int]$fields[0]
            if ($team -lt 1 -or $team -gt 100 -or $seen.ContainsKey($team)) { throw "Ungueltige/doppelte Teamnummer: $team" }
            $seen[$team] = $true
            if ($fields[4] -notmatch '^[0-9a-fA-F]{8}$') { throw "Team $team hat keinen vollstaendigen RGBA-Farbwert: $line" }
            if ($fields | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_.Contains('"') }) { throw "Leeres/zitiertes Feld bei Team $team" }
            $image = New-Object Drawing.Bitmap((Join-Path $folder ('TeamIcon\' + $fields[3])))
            try {
                $visible = $false
                for ($y=0; $y -lt $image.Height -and -not $visible; $y++) {
                    for ($x=0; $x -lt $image.Width; $x++) {
                        if ($image.GetPixel($x,$y).A -gt 128) { $visible = $true; break }
                    }
                }
                if (-not $visible) { throw "Team $team hat ein unsichtbares Icon." }
            } finally { $image.Dispose() }
        }
        Write-Output "Paket $pack, Nummern $numbers : Teams 1-100 inklusive Team 26 mit vollstaendiger CSV und sichtbaren Icons geprueft."
    }
}
