# Generate the two simple tricolours from geometric bands, without external artwork.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$output = Join-Path $PSScriptRoot '..\assets\flags-source'
$flags = @(
    @{ Name = 'ITA'; Vertical = $true; Colors = @('#009246', '#FFFFFF', '#CE2B37') },
    @{ Name = 'NED'; Vertical = $false; Colors = @('#AE1C28', '#FFFFFF', '#21468B') }
)
foreach ($flag in $flags) {
    $bitmap = New-Object Drawing.Bitmap(300, 200)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        for ($i = 0; $i -lt 3; $i++) {
            $brush = New-Object Drawing.SolidBrush([Drawing.ColorTranslator]::FromHtml($flag.Colors[$i]))
            try {
                if ($flag.Vertical) { $graphics.FillRectangle($brush, ($i * 100), 0, 100, 200) }
                else {
                    $top = [int][Math]::Round($i * 200 / 3)
                    $bottom = [int][Math]::Round(($i + 1) * 200 / 3)
                    $graphics.FillRectangle($brush, 0, $top, 300, ($bottom - $top))
                }
            } finally { $brush.Dispose() }
        }
        $bitmap.Save((Join-Path $output ($flag.Name + '.png')), [Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
}
Write-Output 'ITA.png und NED.png als Originale erzeugt: 300 x 200 Pixel, ohne Nummern. Anschliessend optimize-flags.ps1 ausfuehren.'
