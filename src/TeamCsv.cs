using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace PubgObserver
{
    // Parse quoted fields and preserve all existing columns and field values.
    public sealed class TeamCsv
    {
        public string[] Headers;
        public readonly List<string[]> Rows = new List<string[]>();
        private readonly HashSet<int> teams = new HashSet<int>();
        public int Column(string name) { return Array.FindIndex(Headers, h => h.Equals(name, StringComparison.OrdinalIgnoreCase)); }
        public int[] Missing { get { return Enumerable.Range(1, 100).Where(n => !teams.Contains(n)).ToArray(); } }

        public static TeamCsv Read(Stream stream)
        {
            var result = new TeamCsv();
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, true), true))
            using (var parser = new TextFieldParser(reader))
            {
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = false;
                if (parser.EndOfData) throw new IOException(Language.Text("emptyCsv"));
                result.Headers = parser.ReadFields();
                if (result.Column("TeamNumber") < 0 || result.Column("ImageFileName") < 0 ||
                    result.Headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.Headers.Length)
                    throw new IOException(Language.Text("csvColumns"));
                while (!parser.EndOfData)
                {
                    var row = parser.ReadFields();
                    int team;
                    if (row.Length != result.Headers.Length || !Int32.TryParse(row[result.Column("TeamNumber")], out team) || team < 1 || !result.teams.Add(team))
                        throw new IOException(Language.Text("csvRow"));
                    result.Rows.Add(row);
                }
            }
            return result;
        }

        public static int MissingInFolder(string folder)
        {
            using (var stream = File.OpenRead(Path.Combine(folder, "TeamInfo.csv"))) return Read(stream).Missing.Length;
        }

        public static int MissingInPack(int index)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Packs." + Installer.PackIds[index] + ".zip"))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            using (var csv = archive.Entries.Single(e => e.FullName.Equals("TeamInfo.csv", StringComparison.OrdinalIgnoreCase)).Open())
                return Read(csv).Missing.Length;
        }

        public static void FillMissing(string folder)
        {
            string csvPath = Path.Combine(folder, "TeamInfo.csv");
            TeamCsv table;
            using (var stream = File.OpenRead(csvPath)) table = Read(stream);
            int[] missing = table.Missing;
            if (missing.Length == 0) return;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Packs.emojis.zip"))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                TeamCsv emojis;
                using (var csv = archive.Entries.Single(e => e.FullName.Equals("TeamInfo.csv", StringComparison.OrdinalIgnoreCase)).Open()) emojis = Read(csv);
                foreach (int team in missing)
                {
                    var emoji = emojis.Rows.Single(r => Int32.Parse(r[emojis.Column("TeamNumber")]) == team);
                    string imageName = emoji[emojis.Column("ImageFileName")];
                    var entry = archive.Entries.Single(e => e.FullName.Replace('\\', '/').Equals("TeamIcon/" + imageName, StringComparison.OrdinalIgnoreCase));
                    // Never overwrite an existing pack image, even if names collide.
                    string filename = "observer-emoji-" + team + ".png";
                    string path = Path.Combine(folder, "TeamIcon", filename);
                    for (int suffix = 1; File.Exists(path) || Directory.Exists(path); suffix++)
                    {
                        filename = "observer-emoji-" + team + "-" + suffix + ".png";
                        path = Path.Combine(folder, "TeamIcon", filename);
                    }
                    using (var input = entry.Open())
                    using (var output = new FileStream(path, FileMode.CreateNew)) input.CopyTo(output);
                    var row = new string[table.Headers.Length];
                    for (int i = 0; i < row.Length; i++)
                    {
                        int column = emojis.Column(table.Headers[i]);
                        if (column < 0 && table.Headers[i].Equals("TeamTags", StringComparison.OrdinalIgnoreCase)) column = emojis.Column("TeamShortName");
                        row[i] = column < 0 ? "" : emoji[column];
                    }
                    row[table.Column("ImageFileName")] = filename;
                    table.Rows.Add(row);
                }
            }
            table.Write(csvPath);
        }

        public void Write(string csvPath)
        {
            using (var writer = new StreamWriter(csvPath, false, new UTF8Encoding(false)))
            {
                writer.WriteLine(String.Join(",", Headers.Select(Quote)));
                foreach (var row in Rows) writer.WriteLine(String.Join(",", row.Select(Quote)));
            }
        }

        private static string Quote(string field)
        {
            // Keep PUBG's plain headers, numeric IDs and filenames unchanged.
            // Only custom values containing CSV delimiters need escaping.
            if (field.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return field;
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
    }
}
