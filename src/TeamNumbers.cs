using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace PubgObserver
{
    public static class TeamNumbers
    {
        public static Bitmap Render(Image image, int team)
        {
            const int size = 128;
            var output = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(output))
            {
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                float ratio = Math.Min((float)size / image.Width, (float)size / image.Height);
                float width = image.Width * ratio;
                float height = image.Height * ratio;
                graphics.DrawImage(image, (size - width) / 2, (size - height) / 2, width, height);
                string text = team.ToString(CultureInfo.InvariantCulture);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = new GraphicsPath())
                using (var family = new FontFamily("Arial"))
                {
                    path.AddString(text, family, (int)FontStyle.Bold, 54, Point.Empty, StringFormat.GenericTypographic);
                    var bounds = path.GetBounds();
                    float textScale = Math.Min(1f, 88f / bounds.Width);
                    using (var transform = new Matrix())
                    {
                        transform.Translate(-bounds.X, -bounds.Y);
                        path.Transform(transform);
                        transform.Reset();
                        transform.Scale(textScale, textScale);
                        path.Transform(transform);
                        bounds = path.GetBounds();
                        transform.Reset();
                        transform.Translate(size - 5 - bounds.Width, size - 5 - bounds.Height);
                        path.Transform(transform);
                    }
                    using (var outline = new Pen(Color.Black, 5) { LineJoin = LineJoin.Round }) graphics.DrawPath(outline, path);
                    graphics.FillPath(Brushes.White, path);
                }
            }
            return output;
        }

        public static void Apply(string folder)
        {
            string csvPath = Path.Combine(folder, "TeamInfo.csv");
            TeamCsv table;
            using (var stream = File.OpenRead(csvPath)) table = TeamCsv.Read(stream);
            string iconRoot = Path.GetFullPath(Path.Combine(folder, "TeamIcon")) + Path.DirectorySeparatorChar;
            foreach (var row in table.Rows)
            {
                string originalPath = Path.GetFullPath(Path.Combine(iconRoot, row[table.Column("ImageFileName")]));
                if (!originalPath.StartsWith(iconRoot, StringComparison.OrdinalIgnoreCase)) throw new IOException(Language.Text("badPath"));
                int team = Int32.Parse(row[table.Column("TeamNumber")]);
                string filename = "observer-numbered-" + team + ".png";
                string path = Path.Combine(iconRoot, filename);
                for (int suffix = 1; File.Exists(path) || Directory.Exists(path); suffix++)
                {
                    filename = "observer-numbered-" + team + "-" + suffix + ".png";
                    path = Path.Combine(iconRoot, filename);
                }
                using (var image = Image.FromFile(originalPath))
                using (var numbered = Render(image, team)) numbered.Save(path, ImageFormat.Png);
                row[table.Column("ImageFileName")] = filename;
            }
            table.Write(csvPath);
        }
    }
}
