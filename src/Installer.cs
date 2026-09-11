using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("PUBG Observer Installer")]
[assembly: AssemblyDescription("Installiert lokale Observer-Pakete fuer PUBG")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace PubgObserver
{
    public static class Installer
    {
        public static readonly string[] PackIds = { "flags-with-numbers", "emojis", "numbers" };
        public static readonly string[] PackNames = { "Flaggen mit Nummern (Standard)", "Emojis", "Nummern" };

        public static string InstallPack(int index, string target)
        {
            if (index < 0 || index >= PackIds.Length) throw new ArgumentOutOfRangeException("index");
            string temporary = Path.Combine(Path.GetTempPath(), "pubg-observer-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporary);
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Packs." + PackIds[index] + ".zip"))
                {
                    if (stream == null) throw new IOException("Das ausgewaehlte Paket fehlt in dieser EXE.");
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                        foreach (var entry in archive.Entries)
                        {
                            string path = Path.GetFullPath(Path.Combine(temporary, entry.FullName));
                            if (!path.StartsWith(temporary + "\\", StringComparison.OrdinalIgnoreCase))
                                throw new IOException("Ungueltiger Pfad im Paket.");
                            if (String.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(path); continue; }
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                            using (var input = entry.Open())
                            using (var output = new FileStream(path, FileMode.CreateNew)) input.CopyTo(output);
                        }
                }
                return Install(temporary, target);
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
                    throw new IOException("Verknuepfte Ordner werden nicht unterstuetzt: " + item.FullName);
        }

        private static void CopyTree(string source, string destination)
        {
            CheckLinks(source);
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Verknuepfte Dateien werden nicht unterstuetzt: " + file);
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), false);
            }
            foreach (string dir in Directory.GetDirectories(source))
                CopyTree(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }

        public static string Install(string source, string target)
        {
            source = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
            target = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar);
            if (source.Equals(target, StringComparison.OrdinalIgnoreCase) ||
                source.StartsWith(target + "\\", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith(source + "\\", StringComparison.OrdinalIgnoreCase))
                throw new IOException("Quell- und Zielordner duerfen sich nicht ueberlappen.");
            if (!File.Exists(Path.Combine(source, "TeamInfo.csv")) || !Directory.Exists(Path.Combine(source, "TeamIcon")))
                throw new IOException("Bitte den Observer-Ordner mit TeamInfo.csv und TeamIcon auswaehlen.");
            CheckLinks(target);
            string parent = Path.GetDirectoryName(target);
            Directory.CreateDirectory(parent);
            string suffix = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string staging = Path.Combine(parent, "Observer-staging-" + suffix);
            string backup = Path.Combine(parent, "Observer-backup-" + suffix);
            // Copy completely before touching the existing installation.
            try { CopyTree(source, staging); }
            catch (Exception ex) { throw new IOException("Kopieren fehlgeschlagen. Bestehende Dateien bleiben erhalten. Teilkopie: " + staging, ex); }
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

        public MainForm()
        {
            Text = "PUBG Observer Installer 1.0.0";
            ClientSize = new Size(640, 455);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(245, 247, 250);
            var title = new Label { Text = "Observer-Dateien installieren", Font = new Font("Segoe UI", 19, FontStyle.Bold), AutoSize = true, Location = new Point(24, 24) };
            var intro = new Label { Text = "Paket auswaehlen und installieren. Alle drei Pakete sind enthalten.\nBitte PUBG vorher schliessen.", Location = new Point(26, 76), Size = new Size(590, 44) };
            packs.Name = "PackSelection";
            packs.DropDownStyle = ComboBoxStyle.DropDownList;
            packs.SetBounds(26, 132, 588, 30);
            packs.Items.AddRange(Installer.PackNames);
            packs.Items.Add("Eigener Observer-Ordner ...");
            source.SetBounds(26, 180, 453, 30);
            source.ReadOnly = true;
            var browse = new Button { Text = "Auswaehlen ...", Location = new Point(489, 178), Size = new Size(125, 32) };
            browse.Click += delegate
            {
                using (var dialog = new FolderBrowserDialog { Description = "Observer-Ordner mit TeamInfo.csv und TeamIcon auswaehlen", ShowNewFolderButton = false })
                    if (dialog.ShowDialog(this) == DialogResult.OK) source.Text = dialog.SelectedPath;
            };
            packs.SelectedIndexChanged += delegate { source.Visible = browse.Visible = packs.SelectedIndex == 3; };
            packs.SelectedIndex = 0;
            var target = new Label { Text = "Ziel: %LOCALAPPDATA%\\TslGame\\Saved\\Observer\n\nVorhandene Observer-Dateien werden ersetzt und vorher automatisch\nin einem separaten Backup-Ordner gesichert.", Location = new Point(26, 236), Size = new Size(588, 90) };
            install.Text = "Installieren";
            install.SetBounds(26, 335, 180, 40);
            install.Click += InstallClick;
            status.SetBounds(26, 389, 588, 55);
            Controls.AddRange(new Control[] { title, intro, packs, source, browse, target, install, status });
        }

        private void InstallClick(object sender, EventArgs args)
        {
            try
            {
                if (packs.SelectedIndex == 3 && String.IsNullOrWhiteSpace(source.Text)) throw new IOException("Bitte zuerst einen Observer-Ordner auswaehlen.");
                var running = Process.GetProcessesByName("TslGame");
                bool gameRunning = running.Length > 0;
                foreach (var process in running) process.Dispose();
                if (gameRunning) throw new IOException("PUBG laeuft noch. Bitte das Spiel schliessen und erneut installieren.");
                install.Enabled = false;
                UseWaitCursor = true;
                string backup = packs.SelectedIndex == 3 ? Installer.Install(source.Text, Installer.DefaultTarget) : Installer.InstallPack(packs.SelectedIndex, Installer.DefaultTarget);
                status.Text = "Installation abgeschlossen. Du kannst PUBG jetzt starten.";
                MessageBox.Show(this, "Observer-Dateien installiert." + (backup == null ? "" : "\n\nSicherung:\n" + backup), "Fertig", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                status.Text = "Installation nicht abgeschlossen.";
                MessageBox.Show(this, ex.Message, "Installation nicht moeglich", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { install.Enabled = true; UseWaitCursor = false; }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
