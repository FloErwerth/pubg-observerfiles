# Team 29: Stand der Diagnose

- Nutzerbeobachtung: Nach Version 1.2.3 zeigt Team 26 ein Emoji, Team 29 weiterhin ein weißes Symbol mit grauem Balken.
- Die installierte CSV und alle 100 referenzierten Bilder wurden bytegenau mit einer frischen Installation von v1.2.3 verglichen; keine Abweichungen.
- Team 29 hat eine eindeutige CSV-Zeile, Farbwert `ffffffff` und ein lesbares, sichtbares Katzenbild mit Nummer 29. Das gemeldete Symbol kommt in keiner der 100 installierten Bilddateien vor.
- Kontrollierter Spieltest: Nur `ImageFileName` in Zeile 29 wurde von `observer-numbered-29.png` auf die bei Team 26 funktionierende Datei `observer-numbered-26.png` umgestellt. Nutzer startete PUBG ohne Neuinstallation; bei Team 29 erschien weiterhin dasselbe weiße Symbol. Der Testverweis war danach weiterhin vorhanden.
- Nach dem Test war PUBG geschlossen; die ursprüngliche CSV wurde über die gesicherte Datei wiederhergestellt.

Das Ergebnis spricht gegen einen Defekt ausschließlich im Katzenbild. Es beweist noch nicht, warum PUBG diese Anzeige nicht aus der erwarteten CSV-Zeile erzeugt. Die Beobachtung bei Team 26 allein beweist auch nicht abschließend die bisher vermutete Wirkung der Farbkorrektur.

Der Nutzer hat die Beobachtung im Replay bestätigt. Noch zu klären: genaue Anzeigestelle innerhalb des Replays (Killfeed, Spieler-/Teamübersicht oder andere UI), eigenes oder fremdes Team und zuverlässige Zuordnung des Symbols zur internen Teamnummer. Ein vollständiger Screenshot der Replay-Oberfläche ist dafür hilfreicher als der bisherige isolierte Symbolausschnitt. Weitere Änderungen am Installer erst anhand eines unterscheidenden Tests; Dateiprüfungen bilden den PUBG-Parser nicht nach.

## Vollständiger Screenshot

Die betroffene Anzeige ist die linke Replay-Spieler-/Teamliste, sortiert nach `DISTANCE`, bei 0:09. Die darüberliegenden Gruppen zeigen Flaggen sowie weiße Balken neben den Spielernamen; in der untersten betroffenen Gruppe fehlen auch diese Balken. Der Nutzer berichtet zusätzlich die fehlende Kürzel-Sektion. Das ist ein konkreter Hinweis auf unvollständige Spieler-/Teamdarstellung, keine Bestätigung eines Bilddateifehlers.

Nächste zu prüfende Hypothese: Für die entfernte Gruppe stehen an diesem Replay-Zeitpunkt nicht alle Daten zur Verfügung. Der offizielle PUBG-Support dokumentiert eine Distanzgrenze von 1 km für das Umschalten auf gespeicherte Spieler im Replay, aber nicht ausdrücklich das hier gezeigte fehlende Team-Icon: https://support.pubg.com/hc/de/articles/115004615754-Gibt-es-Hotkeys-f%C3%BCr-den-Replay-modus

Unterscheidender Test ohne Dateiänderung: Im selben Replay die betroffene Gruppe zu einem späteren Zeitpunkt prüfen, wenn sie näher am aufzeichnenden Spieler ist, und versuchen, einen ihrer Spieler anzuwählen. Beobachten, ob Kürzel/Balken und Emoji gemeinsam erscheinen. Noch kein Ergebnis.
