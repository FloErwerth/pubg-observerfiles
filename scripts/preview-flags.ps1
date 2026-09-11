param([string]$InstallerPath = (Join-Path $PSScriptRoot '..\dist\PUBG-Observer-Installer.exe'))
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -eq 'Core') {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -InstallerPath $InstallerPath
    if ($LASTEXITCODE -ne 0) { throw 'Flaggen-Vorschau fehlgeschlagen.' }
    exit 0
}
Add-Type -AssemblyName System.Drawing,System.Windows.Forms
[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($InstallerPath)) | Out-Null
$canvas = New-Object Drawing.Bitmap(1000,850)
$g = [Drawing.Graphics]::FromImage($canvas)
$font = New-Object Drawing.Font('Segoe UI',10)
$title = New-Object Drawing.Font('Segoe UI',16,[Drawing.FontStyle]::Bold)
try {
    $g.Clear([Drawing.Color]::FromArgb(35,39,46))
    $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawString('Flaggen: einheitliche Groesse und moderate Schaerfung',$title,[Drawing.Brushes]::White,20,12)
    $g.DrawString('Je Feld: vorher / optimiert / mit Nummer (32 px) - darunter optimiert in 96 x 64 px',$font,[Drawing.Brushes]::LightGray,20,46)
    $rows = @(Import-Csv -LiteralPath (Join-Path $PSScriptRoot '..\packages\flags\Observer\TeamInfo.csv'))
    for ($i=0; $i -lt $rows.Count; $i++) {
        $row=$rows[$i]
        $old=[Drawing.Image]::FromFile((Join-Path $PSScriptRoot ('..\assets\flags-source\' + $row.ImageFileName)))
        $new=[Drawing.Image]::FromFile((Join-Path $PSScriptRoot ('..\packages\flags\Observer\TeamIcon\' + $row.ImageFileName)))
        $numbered=[PubgObserver.TeamNumbers]::Render($new,[int]$row.TeamNumber)
        try {
            $x=20+($i%5)*196; $y=85+[Math]::Floor($i/5)*152
            $g.DrawString(($row.TeamNumber + '  ' + $row.TeamShortName),$font,[Drawing.Brushes]::White,$x,$y)
            $ratio=[Math]::Min(32.0/$old.Width,32.0/$old.Height)
            $g.DrawImage($old,[single]$x,[single]($y+30+(32-$old.Height*$ratio)/2),[single]($old.Width*$ratio),[single]($old.Height*$ratio))
            $g.DrawImage($new,[single]($x+55),[single]($y+35.33),[single]32,[single](32*2/3))
            $g.DrawImage($numbered,[single]($x+110),[single]($y+30),[single]32,[single]32)
            $g.DrawImage($new,[single]$x,[single]($y+74),[single]96,[single]64)
        } finally { $old.Dispose(); $new.Dispose(); $numbered.Dispose() }
    }
    $canvas.Save((Join-Path $PSScriptRoot '..\docs\images\flags-preview.png'),[Drawing.Imaging.ImageFormat]::Png)
} finally { $g.Dispose(); $canvas.Dispose(); $font.Dispose(); $title.Dispose() }
