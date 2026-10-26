# Manuskript-Helfer

Ein Analyse-Werkzeug für deutschsprachige Autorinnen und Autoren, die in Word, Markdown oder reinem Text schreiben. Es zeigt dir, wo dein Manuskript holprig wird: häufige Wörter, unnötige Wiederholungen, mögliche Namensvarianten – und wie viele Normseiten du schon hast.

Keine Cloud. Keine Anmeldung. Dein Manuskript bleibt auf deinem Rechner.

**Status:** Beta (0.3.0). Funktioniert, ist aber noch nicht an vielen Manuskripten erprobt. Feedback willkommen.

---

**Warum es das gibt:** Ich schreibe gerade einen Roman und habe ein Tool gesucht, das mir eine Übersicht über Formulierungen gibt und bei der Struktur hilft. Deshalb habe ich dieses Tool entwickelt, um genau das zu ermöglichen.

---

## Was es kann

| Funktion | Was sie dir zeigt |
|---|---|
| **Statistik** | Wörter, Zeichen, Sätze, Ø Satzlänge, Lesezeit und Normseiten – pro Kapitel und als Summe |
| **Wiederholungen** | Wörter, die sich innerhalb eines Absatzes (≈ 100 Wörter) auffällig oft drängen – mit Textstelle |
| **Häufige Wörter** | Die 30 häufigsten Wörter pro Kapitel – ein Hinweis auf Lieblingswörter und Füllwörter |
| **Figuren** | Figuren- und Ortsverzeichnis mit Spitznamen; Auftritte pro Kapitel als Matrix; ähnlich klingende Schreibvarianten als mögliche Tippfehler |
| **Prüfliste** | Treffer, die du als okay oder gewollt markiert hast, tauchen nicht wieder auf |
| **Review-Export** | Alle offenen Treffer als Markdown-Tabelle zum Abarbeiten in Obsidian, VS Code o. ä. |

---

## Für wen es gedacht ist

- Autorinnen und Autoren, die **selbst überarbeiten**, nicht die Rechtschreibprüfung ersetzen wollen.
- Projekte mit **Normseiten-Layout** (A4, 30 Zeilen, 60 Zeichen, Zeilenabstand 1,5), weil die Normseiten-Berechnung dann der Word-Zahl entspricht.
- Alle, die **Dark Romance, Fantasy, Krimi** oder andere Genres schreiben, in denen ein paar Lieblingsadjektive und Figurennamen oft durchlaufen.

Nicht gedacht für: Lektorat, Stilanalyse mit KI, Verlagsformatierung, Zusammenarbeit in Echtzeit.

---

## Voraussetzungen

- **Windows 10 oder 11**
- **.NET 8 SDK** (nur wenn du aus dem Quellcode baust) – Download bei [microsoft.com/net](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Microsoft Word** ist *nicht* nötig – die `.docx` wird direkt gelesen

Unterstützte Dateiformate: `.docx`, `.txt`, `.md`

---

## Installation

### Variante A: Fertige Version herunterladen

1. Lade die aktuelle ZIP aus den [Releases](https://github.com/Gaja85/Manuskript_Helfer/releases).
2. Entpacke sie an einen Ort deiner Wahl.
3. Doppelklick auf `ManuskriptHelfer.exe`.

Windows zeigt beim ersten Start eventuell eine SmartScreen-Warnung (weil die EXE nicht signiert ist). Klick auf „Weitere Informationen" → „Trotzdem ausführen".

### Variante B: Selbst bauen

```bash
git clone https://github.com/Gaja85/Manuskript_Helfer.git
cd Manuskript_Helfer
dotnet restore
dotnet build -c Release