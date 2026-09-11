using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("PUBG Observer Installer")]
[assembly: AssemblyDescription("Installiert lokale Observer-Pakete fuer PUBG")]
[assembly: AssemblyVersion("1.1.1.0")]
[assembly: AssemblyFileVersion("1.1.1.0")]

namespace PubgObserver
{
    public static class Installer
    {
        public static readonly string[] PackIds = { "flags-with-numbers", "emojis", "flags" };
        public static string[] PackNames { get { return new[] { Language.Text("numbered"), Language.Text("emojis"), Language.Text("plain") }; } }
        public static string[] PackCoverage { get { return new[] { Language.Text("coverage50"), Language.Text("coverage100"), Language.Text("coverage25") }; } }

        public static string InstallPack(int index, string target, bool fillWithEmojis = false)
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
                return Install(temporary, target, fillWithEmojis);
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

        public static string Install(string source, string target, bool fillWithEmojis = false)
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
        private readonly Label fillInfo = new Label();
        private bool translating;

        public MainForm()
        {
            Text = "PUBG Observer Installer 1.1.1";
            ClientSize = new Size(640, 570);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(245, 247, 250);
            var title = new Label { Font = new Font("Segoe UI", 19, FontStyle.Bold), AutoSize = true, Location = new Point(24, 24) };
            var intro = new Label { Location = new Point(26, 76), Size = new Size(590, 44) };
            packs.Name = "PackSelection";
            packs.DropDownStyle = ComboBoxStyle.DropDownList;
            packs.SetBounds(26, 132, 588, 30);
            packs.Items.AddRange(Installer.PackNames);
            packs.Items.Add(Language.Text("custom"));
            source.SetBounds(26, 180, 453, 30);
            source.ReadOnly = true;
            source.Name = "SourceFolder";
            source.TextChanged += delegate { UpdateFillOption(); };
            var browse = new Button { Location = new Point(489, 178), Size = new Size(125, 32) };
            browse.Click += delegate
            {
                using (var dialog = new FolderBrowserDialog { Description = Language.Text("folder"), ShowNewFolderButton = false })
                    if (dialog.ShowDialog(this) == DialogResult.OK) source.Text = dialog.SelectedPath;
            };
            var coverage = new Label { Name = "PackCoverage", Location = new Point(26, 180), Size = new Size(588, 44) };
            packs.SelectedIndexChanged += delegate
            {
                if (translating) return;
                bool custom = packs.SelectedIndex == 3;
                source.Visible = browse.Visible = custom;
                coverage.Visible = !custom;
                coverage.Text = custom || packs.SelectedIndex < 0 ? "" : Installer.PackCoverage[packs.SelectedIndex];
                UpdateFillOption();
            };
            fill.Name = "FillMissing";
            fill.SetBounds(26, 228, 588, 28);
            fillInfo.SetBounds(26, 260, 588, 38);
            packs.SelectedIndex = 0;
            var target = new Label { Location = new Point(26, 306), Size = new Size(588, 90) };
            install.SetBounds(26, 405, 180, 40);
            install.Click += InstallClick;
            status.SetBounds(26, 459, 588, 55);
            Controls.AddRange(new Control[] { title, intro, packs, coverage, source, browse, fill, fillInfo, target, install, status });
            var languageLabel = new Label { Location = new Point(26, 526), Size = new Size(115, 25) };
            var languages = new ComboBox { Name = "LanguageSelection", DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(150, 522), Size = new Size(240, 30) };
            languages.Items.AddRange(new[] { Language.Text("system"), "English", "Deutsch" });
            languages.SelectedIndex = 0;
            var donate = new Button { Name = "Donate", Location = new Point(414, 508), Size = new Size(200, 56), Tag = "https://buymeacoffee.com/forli69", FlatStyle = FlatStyle.Flat, BackgroundImageLayout = ImageLayout.Zoom, Cursor = Cursors.Hand, UseVisualStyleBackColor = false, BackColor = BackColor };
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
                    intro.Text = Language.Text("intro");
                    browse.Text = Language.Text("browse");
                    fill.Text = Language.Text("fill");
                    target.Text = Language.Text("target");
                    install.Text = Language.Text("install");
                    languageLabel.Text = Language.Text("language");
                    donate.AccessibleName = Language.Text("donate");
                    languages.Items[0] = Language.Text("system");
                    packs.Items.Clear();
                    packs.Items.AddRange(Installer.PackNames);
                    packs.Items.Add(Language.Text("custom"));
                    packs.SelectedIndex = selected;
                    coverage.Text = selected < 0 || selected == 3 ? "" : Installer.PackCoverage[selected];
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

        private void UpdateFillOption()
        {
            fill.Checked = false;
            fill.Visible = false;
            fill.Enabled = false;
            fillInfo.Text = "";
            try
            {
                if (packs.SelectedIndex < 0 || (packs.SelectedIndex == 3 && String.IsNullOrWhiteSpace(source.Text))) return;
                int missing = packs.SelectedIndex == 3 ? TeamCsv.MissingInFolder(source.Text) : TeamCsv.MissingInPack(packs.SelectedIndex);
                fill.Visible = fill.Enabled = missing > 0;
                fillInfo.Text = missing > 0 ? String.Format(Language.Text("missing"), missing) : Language.Text("complete");
            }
            catch (Exception ex) { fillInfo.Text = Language.Text("csvCheck") + ex.Message; }
        }

        private void InstallClick(object sender, EventArgs args)
        {
            try
            {
                if (packs.SelectedIndex == 3 && String.IsNullOrWhiteSpace(source.Text)) throw new IOException(Language.Text("chooseFirst"));
                var running = Process.GetProcessesByName("TslGame");
                bool gameRunning = running.Length > 0;
                foreach (var process in running) process.Dispose();
                if (gameRunning) throw new IOException(Language.Text("running"));
                install.Enabled = false;
                UseWaitCursor = true;
                bool fillMissing = fill.Enabled && fill.Checked;
                string backup = packs.SelectedIndex == 3 ? Installer.Install(source.Text, Installer.DefaultTarget, fillMissing) : Installer.InstallPack(packs.SelectedIndex, Installer.DefaultTarget, fillMissing);
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
