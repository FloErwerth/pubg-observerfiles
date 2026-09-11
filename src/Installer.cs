using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("PUBG Observer Installer")]
[assembly: AssemblyDescription("Installiert lokale Observer-Pakete fuer PUBG")]
[assembly: AssemblyVersion("1.2.3.0")]
[assembly: AssemblyFileVersion("1.2.3.0")]

namespace PubgObserver
{
    public static class Installer
    {
        public static readonly string[] PackIds = { "flags", "emojis" };
        public static string[] PackNames { get { return new[] { Language.Text("plain"), Language.Text("emojis") }; } }

        public static string InstallPack(int index, string target, bool fillWithEmojis = false, bool addNumbers = false)
        {
            if (index < 0 || index >= PackIds.Length) throw new ArgumentOutOfRangeException("index");
            string temporary = Path.Combine(Path.GetTempPath(), "pubg-observer-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporary);
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Packs." + PackIds[index] + ".zip"))
                {
                    if (stream == null) throw new IOException(Language.Text("missingPack"));
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                        foreach (var entry in archive.Entries)
                        {
                            string path = Path.GetFullPath(Path.Combine(temporary, entry.FullName));
                            if (!path.StartsWith(temporary + "\\", StringComparison.OrdinalIgnoreCase))
                                throw new IOException(Language.Text("badPath"));
                            if (String.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(path); continue; }
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                            using (var input = entry.Open())
                            using (var output = new FileStream(path, FileMode.CreateNew)) input.CopyTo(output);
                        }
                }
                return Install(temporary, target, fillWithEmojis, addNumbers);
            }
            finally
            {
                // Only this invocation's randomly named extraction directory is removed.
                try { Directory.Delete(temporary, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        public static string DefaultTarget
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"TslGame\Saved\Observer"); }
        }

        private static void CheckLinks(string path)
        {
            for (var item = new DirectoryInfo(path); item != null; item = item.Parent)
                if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException(Language.Text("linkedFolder") + item.FullName);
        }

        private static void CopyTree(string source, string destination)
        {
            CheckLinks(source);
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException(Language.Text("linkedFile") + file);
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), false);
            }
            foreach (string dir in Directory.GetDirectories(source))
                CopyTree(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }

        public static string Install(string source, string target, bool fillWithEmojis = false, bool addNumbers = false)
        {
            source = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
            target = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar);
            if (source.Equals(target, StringComparison.OrdinalIgnoreCase) ||
                source.StartsWith(target + "\\", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith(source + "\\", StringComparison.OrdinalIgnoreCase))
                throw new IOException(Language.Text("overlap"));
            if (!File.Exists(Path.Combine(source, "TeamInfo.csv")) || !Directory.Exists(Path.Combine(source, "TeamIcon")))
                throw new IOException(Language.Text("folder"));
            CheckLinks(target);
            string parent = Path.GetDirectoryName(target);
            Directory.CreateDirectory(parent);
            string suffix = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string staging = Path.Combine(parent, "Observer-staging-" + suffix);
            string backup = Path.Combine(parent, "Observer-backup-" + suffix);
            // Copy completely before touching the existing installation.
            try
            {
                CopyTree(source, staging);
                if (fillWithEmojis) TeamCsv.FillMissing(staging);
                if (addNumbers) TeamNumbers.Apply(staging);
            }
            catch (Exception ex) { throw new IOException(Language.Text("copyFailed") + staging, ex); }
            bool existed = Directory.Exists(target);
            if (existed) Directory.Move(target, backup);
            try { Directory.Move(staging, target); }
            catch
            {
                if (existed && !Directory.Exists(target)) Directory.Move(backup, target);
                throw;
            }
            return existed ? backup : null;
        }
    }

    public sealed class MainForm : Form
    {
        private readonly TextBox source = new TextBox();
        private readonly ComboBox packs = new ComboBox();
        private readonly Label status = new Label();
        private readonly Button install = new Button();
        private readonly CheckBox fill = new CheckBox();
        private readonly CheckBox numbers = new CheckBox();
        private bool translating;

        public MainForm()
        {
            Text = "PUBG Observer Installer 1.2.3";
            ClientSize = new Size(700, 550);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            DoubleBuffered = true;
            BackColor = Color.FromArgb(242, 244, 248);
            ForeColor = Color.FromArgb(30, 41, 59);
            var muted = Color.FromArgb(100, 116, 139);
            var headerColor = Color.FromArgb(20, 29, 44);
            Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float scale = ClientSize.Width / 700f;
                e.Graphics.ScaleTransform(scale, scale);
                using (var brush = new SolidBrush(headerColor)) e.Graphics.FillRectangle(brush, 0, 0, 700, 104);
                using (var accent = new SolidBrush(Color.FromArgb(255, 207, 51))) e.Graphics.FillRectangle(accent, 32, 0, 56, 4);
                DrawCard(e.Graphics, new Rectangle(32, 124, 636, 228));
                DrawCard(e.Graphics, new Rectangle(32, 372, 636, 88));
                using (var line = new Pen(Color.FromArgb(218, 224, 233))) e.Graphics.DrawLine(line, 32, 488, 668, 488);
            };
            var brand = new Label { Text = "PUBG  /  OBSERVER TOOLS", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(255, 207, 51), BackColor = headerColor, Location = new Point(32, 21), Size = new Size(620, 20) };
            var title = new Label { Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = Color.White, BackColor = headerColor, AutoSize = true, Location = new Point(28, 43) };
            var packLabel = new Label { Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = muted, BackColor = Color.White, Location = new Point(52, 142), Size = new Size(580, 22) };
            packs.Name = "PackSelection";
            packs.DropDownStyle = ComboBoxStyle.DropDownList;
            packs.SetBounds(52, 172, 596, 32);
            packs.Font = new Font("Segoe UI", 11);
            packs.FlatStyle = FlatStyle.Flat;
            packs.BackColor = Color.FromArgb(246, 248, 251);
            packs.Items.AddRange(Installer.PackNames);
            packs.Items.Add(Language.Text("custom"));
            source.SetBounds(52, 216, 464, 30);
            source.ReadOnly = true;
            source.Name = "SourceFolder";
            source.TextChanged += delegate { UpdateFillOption(); };
            var browse = new Button { Location = new Point(528, 214), Size = new Size(120, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = Cursors.Hand };
            browse.FlatAppearance.BorderColor = Color.FromArgb(207, 215, 226);
            browse.Click += delegate
            {
                using (var dialog = new FolderBrowserDialog { Description = Language.Text("folder"), ShowNewFolderButton = false })
                    if (dialog.ShowDialog(this) == DialogResult.OK) source.Text = dialog.SelectedPath;
            };
            packs.SelectedIndexChanged += delegate
            {
                if (translating) return;
                bool custom = packs.SelectedIndex == Installer.PackIds.Length;
                source.Visible = browse.Visible = custom;
                UpdateFillOption();
            };
            fill.Name = "FillMissing";
            fill.SetBounds(52, 258, 596, 28);
            fill.BackColor = Color.White;
            fill.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            numbers.Name = "AddNumbers";
            numbers.Checked = true;
            numbers.SetBounds(52, 300, 596, 28);
            numbers.BackColor = Color.White;
            numbers.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            packs.SelectedIndex = 0;
            var target = new Label { Location = new Point(52, 390), Size = new Size(380, 52), BackColor = Color.White, Font = new Font("Segoe UI", 9), ForeColor = muted };
            install.SetBounds(460, 392, 188, 48);
            install.BackColor = headerColor;
            install.ForeColor = Color.White;
            install.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            install.FlatStyle = FlatStyle.Flat;
            install.FlatAppearance.BorderSize = 0;
            install.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 61, 84);
            install.Cursor = Cursors.Hand;
            install.Click += InstallClick;
            status.SetBounds(32, 463, 636, 24);
            status.Font = new Font("Segoe UI", 9);
            Controls.AddRange(new Control[] { brand, title, packLabel, packs, source, browse, fill, numbers, target, install, status });
            var languageLabel = new Label { Location = new Point(32, 510), Size = new Size(86, 25), ForeColor = muted, Font = new Font("Segoe UI", 9) };
            var languages = new ComboBox { Name = "LanguageSelection", DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(118, 504), Size = new Size(216, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.White };
            languages.Items.AddRange(new[] { Language.Text("system"), "English", "Deutsch" });
            languages.SelectedIndex = 0;
            var donate = new Button { Name = "Donate", Location = new Point(508, 498), Size = new Size(160, 45), Tag = "https://buymeacoffee.com/forli69", FlatStyle = FlatStyle.Flat, BackgroundImageLayout = ImageLayout.Zoom, Cursor = Cursors.Hand, UseVisualStyleBackColor = false, BackColor = BackColor };
            donate.FlatAppearance.BorderSize = 0;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Brand.BuyMeACoffee.png"))
            using (var graphic = Image.FromStream(stream)) donate.BackgroundImage = new Bitmap(graphic);
            donate.Disposed += delegate { donate.BackgroundImage.Dispose(); };
            donate.Click += delegate
            {
                try { Process.Start(new ProcessStartInfo((string)donate.Tag) { UseShellExecute = true }); }
                catch (Exception) { MessageBox.Show(this, Language.Text("browserError"), "Buy Me a Coffee", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            };
            Controls.AddRange(new Control[] { languageLabel, languages, donate });
            Action applyLanguage = delegate
            {
                bool wasChecked = fill.Checked;
                int selected = packs.SelectedIndex;
                translating = true;
                try
                {
                    title.Text = Language.Text("title");
                    packLabel.Text = Language.Text("packLabel");
                    browse.Text = Language.Text("browse");
                    fill.Text = Language.Text("fill");
                    numbers.Text = Language.Text("numbers");
                    target.Text = Language.Text("target");
                    install.Text = Language.Text("install");
                    languageLabel.Text = Language.Text("language");
                    donate.AccessibleName = Language.Text("donate");
                    languages.Items[0] = Language.Text("system");
                    packs.Items.Clear();
                    packs.Items.AddRange(Installer.PackNames);
                    packs.Items.Add(Language.Text("custom"));
                    packs.SelectedIndex = selected;
                    status.Text = "";
                    UpdateFillOption();
                    fill.Checked = fill.Enabled && wasChecked;
                }
                finally { translating = false; }
            };
            languages.SelectedIndexChanged += delegate
            {
                if (translating) return;
                Language.Select(languages.SelectedIndex == 0 ? "system" : languages.SelectedIndex == 2 ? "de" : "en");
                applyLanguage();
            };
            applyLanguage();
        }

        private static void DrawCard(Graphics graphics, Rectangle bounds)
        {
            const int radius = 16;
            using (var path = new GraphicsPath())
            {
                path.AddArc(bounds.Left, bounds.Top, radius, radius, 180, 90);
                path.AddArc(bounds.Right - radius, bounds.Top, radius, radius, 270, 90);
                path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                using (var brush = new SolidBrush(Color.White)) graphics.FillPath(brush, path);
                using (var pen = new Pen(Color.FromArgb(225, 230, 237))) graphics.DrawPath(pen, path);
            }
        }

        private void UpdateFillOption()
        {
            bool custom = packs.SelectedIndex == Installer.PackIds.Length;
            fill.Top = custom ? 258 : 222;
            numbers.Top = fill.Top + 42;
            fill.Checked = false;
            fill.Visible = false;
            fill.Enabled = false;
            status.Text = "";
            try
            {
                if (packs.SelectedIndex < 0 || (packs.SelectedIndex == Installer.PackIds.Length && String.IsNullOrWhiteSpace(source.Text))) return;
                int missing = packs.SelectedIndex == Installer.PackIds.Length ? TeamCsv.MissingInFolder(source.Text) : TeamCsv.MissingInPack(packs.SelectedIndex);
                fill.Visible = fill.Enabled = missing > 0;
            }
            catch (Exception ex) { status.Text = Language.Text("csvCheck") + ex.Message; }
        }

        private void InstallClick(object sender, EventArgs args)
        {
            try
            {
                if (packs.SelectedIndex == Installer.PackIds.Length && String.IsNullOrWhiteSpace(source.Text)) throw new IOException(Language.Text("chooseFirst"));
                var running = Process.GetProcessesByName("TslGame");
                bool gameRunning = running.Length > 0;
                foreach (var process in running) process.Dispose();
                if (gameRunning) throw new IOException(Language.Text("running"));
                install.Enabled = false;
                UseWaitCursor = true;
                bool fillMissing = fill.Enabled && fill.Checked;
                string backup = packs.SelectedIndex == Installer.PackIds.Length ? Installer.Install(source.Text, Installer.DefaultTarget, fillMissing, numbers.Checked) : Installer.InstallPack(packs.SelectedIndex, Installer.DefaultTarget, fillMissing, numbers.Checked);
                status.Text = Language.Text("success");
                MessageBox.Show(this, Language.Text("installed") + (backup == null ? "" : "\n\n" + Language.Text("backup") + "\n" + backup), Language.Text("done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                status.Text = Language.Text("failed");
                MessageBox.Show(this, ex.Message, Language.Text("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { install.Enabled = true; UseWaitCursor = false; }
        }

        [STAThread]
        public static void Main()
        {
            Language.Select("system");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
