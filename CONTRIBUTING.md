# Mitwirken

Beiträge sind willkommen — Fehlerberichte, Feature-Wünsche, Code.

## Fehler melden

Bitte über die [Issue-Vorlage](https://github.com/Gaja85/Manuskript_Helfer/issues/new?template=bug_report.md). Wenn möglich mit:

- Betriebssystem und .NET-Version
- Dateiendung des geladenen Manuskripts (`.docx` / `.txt` / `.md`)
- Screenshot der Fehlermeldung
- (wenn die Datei nicht privat ist) ein Minimalbeispiel, das den Fehler auslöst

## Feature vorschlagen

Über die [Feature-Vorlage](https://github.com/Gaja85/Manuskript_Helfer/issues/new?template=feature_request.md). Bitte beschreibe:

- Welches Problem das Feature löst
- Wie du es nutzen würdest
- (optional) Wie es sich in den bestehenden Aufbau einfügt

Bevor du größere Änderungen am Code angehst: kurz ein Issue aufmachen und die Richtung abstimmen. Spart beiden Seiten Zeit.

## Pull Requests

- **Kleine, gezielte PRs** sind besser als große Umbauten.
- **Code-Stil:** dem bestehenden Code folgen (Records, `var`, Expression-Bodied Members, keine Regions).
- **Keine UI-Abhängigkeiten in Services.** Neue Analysen gehören nach `Services/` und kennen keine `System.Windows`-Typen.
- **Keine MVVM-Einführung** ohne vorherige Diskussion.
- **Tests:** Wenn du eine neue Analyse hinzufügst, wäre ein Unit-Test schön. Ein Testprojekt gibt es noch nicht — wenn du eins anlegst, willkommen.

## Fragen

Kein Issue aufmachen, wenn du nur eine Frage hast. Kurze Nachricht an [@Gaja85](https://github.com/Gaja85) reicht.