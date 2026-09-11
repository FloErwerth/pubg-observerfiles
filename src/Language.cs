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
            {"intro", new[] {"Choose a pack and install. All three packs are included.\nPlease close PUBG first.", "Paket auswählen und installieren. Alle drei Pakete sind enthalten.\nBitte PUBG vorher schließen."}},
            {"numbered", new[] {"Flags with numbers (default)", "Flaggen mit Nummern (Standard)"}},
            {"emojis", new[] {"Emojis", "Emojis"}},
            {"plain", new[] {"Flags without numbers", "Flaggen ohne Nummern"}},
            {"custom", new[] {"Custom Observer folder…", "Eigener Observer-Ordner…"}},
            {"coverage50", new[] {"Includes images for teams 1-50.", "Enthält Bilder für Teams 1-50."}},
            {"coverage100", new[] {"Includes images for teams 1-100.", "Enthält Bilder für Teams 1-100."}},
            {"coverage25", new[] {"Includes images for teams 1-25. No flag is assigned from team 26 onward.", "Enthält Bilder für Teams 1-25. Ab Team 26 ist keine Flagge definiert."}},
            {"browse", new[] {"Browse…", "Auswählen…"}},
            {"folder", new[] {"Select the Observer folder containing TeamInfo.csv and TeamIcon.", "Observer-Ordner mit TeamInfo.csv und TeamIcon auswählen."}},
            {"fill", new[] {"Fill missing assignments with emojis", "Fehlende Zuordnungen mit Emojis auffüllen"}},
            {"numbers", new[] {"Add readable team numbers", "Gut lesbare Teamnummern hinzufügen"}},
            {"numberInfo", new[] {"White numbers on a dark badge. Disable to use the original images.", "Weiße Zahlen auf dunklem Hintergrund. Ausgeschaltet: Originalbilder."}},
            {"missing", new[] {"{0} missing team numbers up to 100 can be added.", "{0} fehlende Teamnummern bis 100 können ergänzt werden."}},
            {"complete", new[] {"All teams 1-100 are assigned.", "Alle Teams 1-100 sind zugeordnet."}},
            {"csvCheck", new[] {"Could not check the CSV: ", "CSV konnte nicht geprüft werden: "}},
            {"target", new[] {"INSTALLATION FOLDER\n%LOCALAPPDATA%\\TslGame\\Saved\\Observer\n\nYour existing files are backed up automatically\nbefore the selected pack is installed.", "INSTALLATIONSORDNER\n%LOCALAPPDATA%\\TslGame\\Saved\\Observer\n\nDeine vorhandenen Dateien werden vor der\nInstallation automatisch gesichert."}},
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
