param([string]$InstallerPath = (Join-Path $PSScriptRoot '..\dist\PUBG-Observer-Installer.exe'))
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -eq 'Core') {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -InstallerPath $InstallerPath
    if ($LASTEXITCODE -ne 0) { throw 'Vorschau fehlgeschlagen.' }
    exit 0
}
Add-Type -AssemblyName System.Drawing,System.Windows.Forms
[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($InstallerPath)) | Out-Null
$canvas = New-Object Drawing.Bitmap(760,310)
$g = [Drawing.Graphics]::FromImage($canvas)
$font = New-Object Drawing.Font('Segoe UI',11)
try {
    $g.Clear([Drawing.Color]::FromArgb(235,239,245))
    $samples = @(@('flags','CAN.png',1), @('flags','ITA.png',24), @('emojis','50.png',50), @('emojis','100.png',100))
    for ($i=0; $i -lt $samples.Count; $i++) {
        $s=$samples[$i]
        $source = [Drawing.Image]::FromFile((Join-Path $PSScriptRoot ('..\packages\' + $s[0] + '\Observer\TeamIcon\' + $s[1])))
        $numbered = [PubgObserver.TeamNumbers]::Render($source,[int]$s[2])
        try {
            $x=24+$i*184
            $g.DrawString(('Team ' + $s[2]),$font,[Drawing.Brushes]::Black,$x,16)
            $g.DrawImage($numbered,$x,52,128,128)
            $g.DrawImage($numbered,$x,205,32,32)
            $g.DrawImage($numbered,($x+52),209,24,24)
            $g.DrawString('32 px / 24 px',$font,[Drawing.Brushes]::Black,$x,258)
        } finally { $numbered.Dispose(); $source.Dispose() }
    }
    $canvas.Save((Join-Path $PSScriptRoot '..\docs\images\numbered-preview.png'),[Drawing.Imaging.ImageFormat]::Png)
} finally { $g.Dispose(); $canvas.Dispose(); $font.Dispose() }
