using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace PubgObserver
{
    public static class Language
    {
        public static readonly string SystemLanguage = Detect(CultureInfo.CurrentUICulture);
        public static string Current = SystemLanguage;
        public static string Detect(CultureInfo culture) { return culture.TwoLetterISOLanguageName == "de" ? "de" : "en"; }
        public static void Select(string language)
        {
            Current = language == "system" ? SystemLanguage : language == "de" ? "de" : "en";
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Current);
        }
        private static readonly Dictionary<string, string[]> texts = new Dictionary<string, string[]>
        {
            {"title", new[] {"Install observer files", "Observer-Dateien installieren"}},
            {"packLabel", new[] {"CHOOSE YOUR ICON PACK", "DEIN ICON-PAKET"}},
            {"emojis", new[] {"Emojis", "Emojis"}},
            {"plain", new[] {"Flags (default)", "Flaggen (Standard)"}},
            {"custom", new[] {"Custom Observer folder…", "Eigener Observer-Ordner…"}},
            {"browse", new[] {"Browse…", "Auswählen…"}},
            {"folder", new[] {"Select the Observer folder containing TeamInfo.csv and TeamIcon.", "Observer-Ordner mit TeamInfo.csv und TeamIcon auswählen."}},
            {"fill", new[] {"Fill missing assignments with emojis", "Fehlende Zuordnungen mit Emojis auffüllen"}},
            {"numbers", new[] {"Add numbers", "Nummern hinzufügen"}},
            {"csvCheck", new[] {"Could not check the CSV: ", "CSV konnte nicht geprüft werden: "}},
            {"target", new[] {"INSTALLATION FOLDER\n%LOCALAPPDATA%\\TslGame\\Saved\\Observer", "INSTALLATIONSORDNER\n%LOCALAPPDATA%\\TslGame\\Saved\\Observer"}},
            {"install", new[] {"Install", "Installieren"}},
            {"chooseFirst", new[] {"Please select an Observer folder first.", "Bitte zuerst einen Observer-Ordner auswählen."}},
            {"running", new[] {"PUBG is running. Please close the game and try again.", "PUBG läuft noch. Bitte das Spiel schließen und erneut installieren."}},
            {"success", new[] {"Installation complete. You can now start PUBG.", "Installation abgeschlossen. Du kannst PUBG jetzt starten."}},
            {"installed", new[] {"Observer files installed.", "Observer-Dateien installiert."}},
            {"backup", new[] {"Backup:", "Sicherung:"}},
            {"done", new[] {"Done", "Fertig"}},
            {"failed", new[] {"Installation did not complete.", "Installation nicht abgeschlossen."}},
            {"error", new[] {"Unable to install", "Installation nicht möglich"}},
            {"language", new[] {"Language", "Sprache"}},
            {"system", new[] {"System language", "Systemsprache"}},
            {"donate", new[] {"Buy me a coffee", "Buy me a coffee"}},
            {"browserError", new[] {"Could not open the browser. You can visit https://buymeacoffee.com/forli69 manually.", "Der Browser konnte nicht geöffnet werden. Du kannst https://buymeacoffee.com/forli69 manuell aufrufen."}},
            {"missingPack", new[] {"The selected pack is missing from this EXE.", "Das ausgewählte Paket fehlt in dieser EXE."}},
            {"badPath", new[] {"Invalid path in the pack.", "Ungültiger Pfad im Paket."}},
            {"linkedFolder", new[] {"Linked folders are not supported: ", "Verknüpfte Ordner werden nicht unterstützt: "}},
            {"linkedFile", new[] {"Linked files are not supported: ", "Verknüpfte Dateien werden nicht unterstützt: "}},
            {"overlap", new[] {"Source and target folders must not overlap.", "Quell- und Zielordner dürfen sich nicht überlappen."}},
            {"copyFailed", new[] {"Could not prepare the files. Existing files are unchanged. Staging folder: ", "Dateien konnten nicht vorbereitet werden. Bestehende Dateien bleiben erhalten. Zwischenordner: "}},
            {"emptyCsv", new[] {"The team CSV is empty.", "Die Team-CSV ist leer."}},
            {"csvColumns", new[] {"CSV requires unique columns including TeamNumber and ImageFileName.", "CSV benötigt eindeutige Spalten einschließlich TeamNumber und ImageFileName."}},
            {"csvRow", new[] {"Invalid CSV row or duplicate team number.", "Ungültige CSV-Zeile oder doppelte Teamnummer."}}
        };
        public static string Text(string key) { return texts[key][Current == "de" ? 1 : 0]; }
    }
}
