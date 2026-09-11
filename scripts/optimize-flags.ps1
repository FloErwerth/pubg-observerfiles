param([string]$OutputDirectory = (Join-Path $PSScriptRoot '..\packages\flags\Observer\TeamIcon'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
# Always start from the archived originals, never sharpen an already processed PNG.
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class FlagOptimizer {
    public static void Save(string source, string destination) {
        using (var input = new Bitmap(source))
        using (var scaled = new Bitmap(192, 128, PixelFormat.Format32bppArgb)) {
            int left = input.Width, top = input.Height, right = -1, bottom = -1;
            for (int y = 0; y < input.Height; y++)
                for (int x = 0; x < input.Width; x++)
                    if (input.GetPixel(x, y).A > 16) {
                        left = Math.Min(left, x); top = Math.Min(top, y);
                        right = Math.Max(right, x); bottom = Math.Max(bottom, y);
                    }
            if (right < left) throw new InvalidOperationException("Empty flag: " + source);
            using (var g = Graphics.FromImage(scaled))
            using (var attributes = new ImageAttributes()) {
                // A common 3:2 footprint intentionally normalizes national aspect ratios.
                // Mirror at the boundary to avoid a translucent fringe from resampling.
                g.CompositingMode = CompositingMode.SourceCopy;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(input, new Rectangle(0, 0, 192, 128), left, top,
                    right - left + 1, bottom - top + 1, GraphicsUnit.Pixel, attributes);
            }
            using (var output = new Bitmap(192, 128, PixelFormat.Format32bppArgb)) {
                int[] weights = { 1, 2, 1 };
                for (int y = 0; y < scaled.Height; y++)
                    for (int x = 0; x < scaled.Width; x++) {
                        var center = scaled.GetPixel(x, y);
                        double r = 0, g = 0, b = 0;
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++) {
                                var p = scaled.GetPixel(Math.Max(0, Math.Min(191, x + dx)), Math.Max(0, Math.Min(127, y + dy)));
                                int w = weights[dx + 1] * weights[dy + 1];
                                r += p.R * w; g += p.G * w; b += p.B * w;
                            }
                        output.SetPixel(x, y, Color.FromArgb(center.A,
                            Sharpen(center.R, r / 16), Sharpen(center.G, g / 16), Sharpen(center.B, b / 16)));
                    }
                output.Save(destination, ImageFormat.Png);
            }
        }
    }
    static int Sharpen(int value, double blur) {
        double detail = value - blur;
        if (Math.Abs(detail) < 3) return value;
        return (int)Math.Round(Math.Max(0, Math.Min(255, value + 0.45 * detail)));
    }
}
'@
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
foreach ($source in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot '..\assets\flags-source') -Filter '*.png') {
    [FlagOptimizer]::Save($source.FullName, (Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) $source.Name))
}
Write-Output '25 Flaggen aus Originalen optimiert: einheitlich 192 x 128 Pixel, moderate Unschaerfemaskierung.'
