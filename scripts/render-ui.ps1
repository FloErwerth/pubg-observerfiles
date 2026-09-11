param([string]$InstallerPath = (Join-Path $PSScriptRoot '..\dist\PUBG-Observer-Installer.exe'))
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -eq 'Core') {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -InstallerPath $InstallerPath
    if ($LASTEXITCODE -ne 0) { throw 'UI-Rendering fehlgeschlagen.' }
    exit 0
}
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.IO.Compression
[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($InstallerPath)) | Out-Null
[Windows.Forms.Application]::EnableVisualStyles()
$output = Join-Path $PSScriptRoot '..\docs\images'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$form = New-Object PubgObserver.MainForm
try {
    $form.ShowInTaskbar = $false
    $form.Opacity = 0
    $form.Show()
    [Windows.Forms.Application]::DoEvents()
    foreach ($index in @(0, 1, 2)) {
        $form.Controls['PackSelection'].SelectedIndex = $index
        $form.PerformLayout()
        $bitmap = New-Object Drawing.Bitmap($form.Width, $form.Height)
        try {
            $form.DrawToBitmap($bitmap, (New-Object Drawing.Rectangle(0, 0, $form.Width, $form.Height)))
            $bitmap.Save((Join-Path $output "installer-$index.png"), [Drawing.Imaging.ImageFormat]::Png)
        } finally { $bitmap.Dispose() }
    }
} finally { $form.Dispose() }
Write-Output 'Drei Ansichten aus der echten Windows-Forms-Oberflaeche gerendert.'
