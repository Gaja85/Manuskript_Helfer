Technische Dokumentation: Manuskript-Helfer
Diese Dokumentation richtet sich an Entwicklerinnen und Entwickler, die das Werkzeug erweitern, Fehler beheben oder verstehen wollen, wie es funktioniert. Für die normale Nutzung siehe Benutzeranleitung.

Stand: 0.3.0 (Beta)
Plattform: Windows (WPF)
Runtime: .NET 8

Ziel und Nicht-Ziele
Ziel: Ein lokales Analyse-Werkzeug, das Autoren Hinweise auf mögliche Textprobleme gibt, ohne das Manuskript zu verändern und ohne Cloud-Abhängigkeit.

Nicht-Ziele:

Keine KI-gestützte Stilanalyse.

Kein Lektorat, keine Rechtschreibkorrektur.

Keine Bearbeitung des Manuskripts.

Keine Kollaboration, kein Server.

Die Architektur folgt daraus: reine Lese-Pipeline, flache Zustandshaltung, UI ohne MVVM-Overhead.

Architektur
Drei Schichten, streng getrennt:

text
┌─────────────────────────────────┐
│  UI (WPF)                       │
│  MainWindow.xaml(.cs)           │
├─────────────────────────────────┤
│  Services (Logik)               │
│  ManuscriptLoader               │
│  TextAnalyzer                   │
│  CharacterTracker               │
│  ReviewStateStore               │
│  ReviewExporter                 │
├─────────────────────────────────┤
│  Models (Records)               │
│  Chapter, ChapterStats, …       │
└─────────────────────────────────┘
Regeln:

Models kennen niemanden.

Services kennen Models, aber keine UI-Typen.

UI kennt Models und Services.

Persistenz läuft ausschließlich über Services (kein File.WriteAllText in der UI).

Das macht die Services testbar und die UI austauschbar.

Projektstruktur
text
ManuskriptHelfer/
├── ManuskriptHelfer.csproj
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
├── Models/
│   └── Models.cs
├── Services/
│   ├── ManuscriptLoader.cs
│   ├── TextAnalyzer.cs
│   ├── CharacterTracker.cs
│   ├── ReviewStateStore.cs
│   └── ReviewExporter.cs
└── docs/
    ├── BENUTZERANLEITUNG.md
    └── TECHNISCHE_DOKUMENTATION.md
Alle Models liegen in einer Datei – das ist für ein Projekt dieser Größe Absicht und keine Nachlässigkeit. Bei mehr Modellen (z. B. Kategorien für die Prüfliste) würde ich aufteilen.

Datenmodelle
csharp
public record Chapter(string Title, string Text);

public record ChapterStats(
    string Title, int Words, int Characters, int Sentences,
    double AvgSentenceLength, double ReadingMinutes, double NormPages);

public record RepetitionHit(
    string Chapter, string Word, int Occurrences, string Context);

public class Character
{
    public string Name { get; set; } = "";
    public List<string> Aliases { get; set; } = new();
}

public record NameVariantHit(
    string Chapter, string Found, string SimilarTo,
    int Occurrences, string Context);

public record ReviewedRepetition(string Chapter, string Word);

public record ReviewedVariant(string Chapter, string Found, string SimilarTo);

public class ReviewState
{
    public List<ReviewedRepetition> Repetitions { get; set; } = new();
    public List<ReviewedVariant> Variants { get; set; } = new();
}

public record WordFrequencyHit(string Chapter, string Word, int Count);
Design-Entscheidungen:

Records für alles Unveränderliche (Treffer, Stats): Wert-Semantik, keine versehentlichen Mutationen.

Klassen für Persistenz (Character, ReviewState): System.Text.Json braucht setzbare Properties für die Deserialisierung.

Chapter enthält reinen Text, keine Formatierung, keine Positionen. Wer Positionen braucht, muss sie beim Parsen mitführen – bisher nicht nötig.

ReviewedRepetition referenziert nur (Chapter, Word), nicht die Stelle. Damit bleibt die Prüfliste stabil, auch wenn sich die Textstelle verschiebt.

Services im Detail
ManuscriptLoader
Aufgabe: Datei → List<Chapter>.

Unterstützte Formate: .docx, .txt, .md.

Kapitel-Erkennung:

Format	Überschrift-Kriterium
.docx	Absatzstil Heading1 / berschrift1 / Title / Titel, oder Zeile beginnt mit „Kapitel", „Chapter", „Prolog", „Epilog", „Vorwort", „Nachwort"
.md	Zeile beginnt mit #
.txt	Zeile beginnt mit „Kapitel", „Chapter", „Prolog", „Epilog", „Vorwort", „Nachwort" (und ist kürzer als 60 Zeichen)
Word-Datei: Verwendet DocumentFormat.OpenXml.Packaging.WordprocessingDocument, liest MainDocumentPart.Document.Body.Descendants<W.Paragraph>().

Bekannte Lücke: Tabellen, Fußnoten, Kommentare, nachverfolgte Änderungen werden nicht gelesen. Für die meisten Manuskripte reicht das, für spezielle Layouts (z. B. Kapitel in Tabellen) nicht.

TryGetPageCount(path): Liest ExtendedFilePropertiesPart.Properties.Pages. Gibt null zurück, wenn die Datei keine .docx ist oder die Eigenschaft fehlt. Word aktualisiert diesen Wert nur beim Speichern.

TextAnalyzer
Aufgabe: Pro Kapitel Kennzahlen berechnen und Treffer finden.

Konstanten:

csharp
private const double WordsPerMinute = 200;
private const double CharactersPerNormPage = 1800;
Analyze(Chapter):

Wörter: \p{L}[\p{L}'’\-]* – alle Buchstabenfolgen inkl. Apostroph und Bindestrich.

Sätze: [.!?…]+ als Trenner, dann alle Segmente zählen, die mindestens ein Wort enthalten. Ellipsen werden als ein Trenner betrachtet.

Ø Satzlänge: words / sentences.

Lesezeit: words / 200.

Normseiten: text.Length / 1800.

FindRepetitions(chapter, windowSize=100, minOccurrences=3, minLength=4):

Wörter extrahieren, Positionen pro normalisiertem Wort merken (case-insensitive).

Für jedes Wort die Positionsliste durchlaufen.

Wenn positions[i + k - 1] - positions[i] ≤ windowSize (k = minOccurrences), ist das ein Treffer.

Trefferfenster so weit wie möglich verlängern; danach hinter das Fenster springen (jeder Fund wird pro Stelle einmal gemeldet).

Filter: Stoppwörter (kleine kuratierte Liste deutscher Funktionswörter, ~80 Einträge), Mindestlänge 4.

Komplexität: O(n) pro Kapitel, wobei n = Wortanzahl. Jedes Wort kommt in seine Positionsliste, jede Liste wird einmal durchlaufen.

TopWordsPerChapter(chapters, exclude, topPerChapter=30, minCount=2, minLength=4):

Pro Kapitel Wortfrequenzen zählen.

Stoppwörter, exclude-Menge (Figuren-/Ortsnamen), Wörter unter minLength und unter minCount ignorieren.

Nach Anzahl absteigend, dann alphabetisch sortieren, Top topPerChapter nehmen.

exclude-Menge: Aus CharacterTracker-Daten gebaut, enthält volle Namen, einzelne Namensbestandteile (≥ 4 Zeichen) und Aliase. Verhindert, dass „Lysander" die Häufigkeitsliste dominiert.

CharacterTracker
Aufgabe: Figurenverwaltung und Namenssuche.

Persistenz: figuren.json im JSON-Array-Format, siehe Benutzeranleitung. JsonSerializerOptions erlauben Kommentare, Trailing Commas und Case-Insensitivität.

BuildMatrix(chapters, characters):

Erzeugt eine DataTable, eine Zeile pro Kapitel, eine Spalte pro Figur.

Spalten intern f0, f1, … (damit WPF-Bindings nicht durch Punkte oder Sonderzeichen in Namen brechen). Der echte Name steht in DataColumn.Caption.

Pro Zelle: Anzahl der Regex-Matches des Figuren-Patterns im Kapiteltext.

BuildRegex(character):

Name + Aliases als Alternativen, |-verknüpft.

Längste Alternative zuerst (verhindert, dass „Anna" in „Anna Beispiel" doppelt zählt).

\s+ statt Leerzeichen, damit mehrfache Leerzeichen matchen.

Wortgrenzen via (?<!\p{L}) und (?!\p{L}) – funktioniert auch bei Umlauten.

(?:s|['’]s)? erlaubt Genitiv-s und Apostroph-s.

FindVariants(chapters, characters):

Alle bekannten Namensteile in ein Dictionary<string, string> (lowercase → Original), Mindestlänge 4.

Buckets nach Anfangsbuchstaben: Dictionary<char, List<(string key, string original)>>.

Pro Kapitel: Wörter extrahieren; nur großgeschriebene Wörter ab Länge 4 prüfen.

Genitiv-s entfernen, wenn bekannt → kein Treffer (kein Tippfehler, sondern reguläre Form).

FindClosest(lower, buckets):

Nur Kandidaten mit gleichem Anfangsbuchstaben.

Distanzlimit: 1 bei Namen unter 8 Zeichen, 2 bei längeren.

Längendifferenz größer als Distanzlimit + 1 überspringen.

Levenshtein mit optionalem Genitiv-s-Fallback.

Treffer pro (Fund, Ähnlich) gruppieren, erste Position merken, Kontext bauen.

BuildContext(words, index): 5 Wörter davor, 10 danach, mit „…" eingefasst. Gleiche Logik wie in TextAnalyzer – bewusst dupliziert, um die Services nicht zu verkuppeln; bei einer dritten Verwendung würde ich es in einen Helper ziehen.

Levenshtein(a, b): Klassischer Algorithmus mit zwei Zeilen statt voller Matrix, O(m·n) Zeit, O(min(m,n)) Platz.

Komplexität der Variantensuche: Ohne Bucketing O(Wörter × Namensteile × Namenslänge²). Mit Bucketing ~1/26 der Namensteile pro Wort – bei 300 Namensteilen und 100k Wörtern ein spürbarer Unterschied.

ReviewStateStore
Aufgabe: Prüfliste laden und speichern.

Persistenz: geprueft.json im JSON-Objekt-Format (siehe Models). Wird beim ersten Öffnen als leeres Objekt angelegt.

Robustheit: Load fängt alle Exceptions ab und gibt bei fehlerhafter Datei ein leeres ReviewState zurück. Das ist bewusst – wenn der Nutzer die Datei manuell bearbeitet und dabei kaputt macht, soll die App nicht abstürzen, sondern mit leerer Prüfliste weiterlaufen. Beim nächsten Markieren wird die Datei überschrieben.

ReviewExporter
Aufgabe: Offene Treffer als Markdown-Tabelle.

Format: Zwei Tabellen (Wiederholungen, Varianten), Header mit Manuskriptname und Metadaten. Escaping für | und Zeilenumbrüche.

UTF-8 ohne BOM: Damit der Output auf allen Markdown-Plattformen sauber aussieht. Mit BOM zeigen manche Renderer ein \ufeff an.

UI-Schicht
Kein MVVM. Für ein Werkzeug dieser Größe wäre ViewModel-Aufwand ohne Nutzen. Stattdessen:

Direkte ItemsSource-Zuweisung auf Records/DataTable.

Code-Behind für alle Click-Handler, mit async void wo nötig.

Zustand in privaten Feldern von MainWindow.

Async-Muster:

csharp
private async void OpenButton_Click(object sender, RoutedEventArgs e)
{
    if (_busy) return;
    try
    {
        SetBusy(true);
        var (chapters, stats, repetitions, frequency) = await Task.Run(() => { … });
        // UI-Update im UI-Thread
    }
    finally
    {
        SetBusy(false);
    }
}
_busy verhindert Doppelklicks. SetBusy deaktiviert den Öffnen-Button und setzt den Cursor. Die eigentliche Analyse läuft in Task.Run; die Rückgabe wird automatisch im UI-Thread fortgesetzt.

DataGrid mit dynamischen Spalten (MatrixGrid):

AutoGenerateColumns="True".

AutoGeneratingColumn-Event liest den echten Namen aus DataColumn.Caption und setzt ihn als Header.

Grund: Bindings auf Spaltennamen mit Punkten oder Klammern funktionieren nicht; die interne ID f0 ist robust, der sichtbare Name bleibt frei.

Kontextmenüs:

DataGrid.ContextMenu mit MenuItem-Handlern.

PreviewMouseRightButtonDown auf DataGridRow, um die Zeile zu selektieren, bevor das Kontextmenü aufgeht (sonst wirkt der Rechtsklick auf eine nicht-selektierte Zeile wie ein Klick ins Leere).

SelectionMode="Extended", damit Mehrfachmarkierung funktioniert.

Dateiformate
figuren.json
json
[
  { "Name": "Lysander Falk", "Aliases": ["Lysander", "der Falkner"] },
  { "Name": "Burg Rosenstein", "Aliases": ["Rosenstein"] }
]
UTF-8, ohne BOM empfohlen.

Kommentare und Trailing Commas werden beim Lesen toleriert (ReadCommentHandling.Skip, AllowTrailingCommas), beim Schreiben aber nicht erzeugt.

geprueft.json
json
{
  "Repetitions": [
    { "Chapter": "Kapitel 3", "Word": "Schatten" }
  ],
  "Variants": [
    { "Chapter": "Kapitel 3", "Found": "Lysandar", "SimilarTo": "Lysander" }
  ]
}
Referenzen über (Kapitel, Wort/Name) – stabil gegen Textverschiebungen.

Duplikate werden beim Markieren erkannt und nicht doppelt hinzugefügt.

Review-Markdown
markdown
# Review: manuskriptname

_Erstellt am 15.03.2025 14:23 · 24 Kapitel · 87.532 Wörter · 292 Normseiten_

## Auffällige Wiederholungen (147)

| Kapitel | Wort | Anzahl | Stelle |
|---|---|---:|---|
| Kapitel 3 | Schatten | 5 | … |

## Mögliche Schreibvarianten (12)

| Kapitel | Gefunden | Ähnlich zu | Anzahl | Stelle |
|---|---|---|---:|---|
| Kapitel 7 | Lysandar | Lysander | 2 | … |
UTF-8 ohne BOM.

Pipes in Zellinhalten werden escaped (\|), Zeilenumbrüche durch Leerzeichen ersetzt.

Erweiterungspunkte
Eine neue Analyse hinzufügen
Record in Models/Models.cs.

Methode in Services/TextAnalyzer.cs, Rückgabe List<NeuerTreffer>.

Aufruf im Task.Run in OpenButton_Click.

Tab in MainWindow.xaml, ItemsSource zuweisen.

Wenn es „geprüft" werden soll: Feld in ReviewState, Eintrag in ApplyReviewFilter, Markieren-Handler.

Eine neue Dateiendung unterstützen
In ManuscriptLoader.Load: switch um den neuen Fall erweitern und eine passende ReadXyz-Methode hinzufügen. Die Rückgabe muss eine List<Line> mit IsHeading-Flag sein.

Die Prüfliste pro Manuskript trennen (geplant)
Der sauberste Weg:

ReviewState bekommt ein Feld Manuscript oder die Datei wird pro Manuskript benannt (geprueft_{name}.json).

ReviewStateStore.DefaultPath wird zu einer Methode GetPath(manuscriptName).

OpenButton_Click übergibt den Manuskriptnamen.

Bei Variante „Feld im State": Alte Einträge ohne Manuscript als „global" behandeln, um Migration zu vermeiden.

Einstellbare Schwellen
Aktuell hartkodiert: windowSize=100, minOccurrences=3, topPerChapter=30, minCount=2, CharactersPerNormPage=1800. Wenn Nutzer diese Werte ändern wollen, würde ich sie in eine einstellungen.json ziehen und in OpenButton_Click laden. Alternativ: UI-Elemente pro Tab – mehr Aufwand, mehr Sichtbarkeit.

Bekannte Grenzen und technische Schulden
Bereich	Problem	Auswirkung
Kapitel-Erkennung	Leere Kapitel bei zwei Überschriften in Folge	SplitIntoChapters legt Kapitel mit leerem Text an
DOCX-Lesen	Tabellen, Fußnoten, Kommentare nicht gelesen	Inhalte fehlen in der Analyse
Normseiten	Zeilenumbrüche zählen mit	~2 % zu hoch
Prüfliste	Nicht pro Manuskript getrennt	Überschneidungen beim Wechsel
Variantensuche	Nur großgeschriebene Wörter	Tippfehler am Satzanfang harmloser Wörter
Variantensuche	Erster Buchstabe muss passen	„Bysander" für „Lysander" wird nicht gefunden
UI	async void-Handler	Exceptions landen im Dispatcher statt im Handler; SetBusy(false) im finally mildert das
Tests	Keine Unit-Tests vorhanden	Refactorings unsicher
Tests wären der nächste sinnvolle Schritt, wenn das Projekt wachsen soll. Konkret: TextAnalyzer.FindRepetitions und CharacterTracker.FindVariants sind die Methoden mit den meisten Sonderfällen und dem höchsten Nutzen für Tests. Ein Testprojekt mit xunit und ein paar handgeschriebenen Kapiteln würde die Regressionssicherheit deutlich erhöhen.

Build und Abhängigkeiten
SDK: .NET 8

NuGet:

xml
<PackageReference Include="DocumentFormat.OpenXml" Version="3.1.0" />
Build:

bash
dotnet restore
dotnet build -c Release
Ausgabe: bin/Release/net8.0-windows/ManuskriptHelfer.exe (Single-File-Veröffentlichung wäre möglich, aber noch nicht eingerichtet).

Warum nur Windows: WPF ist Windows-only. Wenn das Projekt plattformübergreifend werden soll, wäre Avalonia der naheliegende Wechsel – das ist aber ein Rewrite der UI-Schicht, keine kleine Änderung.

Mitwirken
Fehler und Wünsche: github.com/Gaja85/Manuskript_Helfer/issues

Pull Requests: Bitte vorher ein Issue aufmachen, damit wir die Richtung abstimmen. Die Services sind mit Absicht frei von UI-Abhängigkeiten – neue Analysen sollten diesem Muster folgen, damit sie testbar bleiben.

Stil: Bestehender Code-Stil gilt (Records, var, Expression-Bodied Members, keine Regions). Keine MVVM-Einführung ohne vorherige Diskussion.