# Benutzeranleitung: Manuskript-Helfer

Diese Anleitung führt dich durch einen typischen Arbeitstag mit dem Manuskript-Helfer. Wenn du das Werkzeug zum ersten Mal startest, lies die Abschnitte **Erste Schritte** und **Die Tabs** – danach kannst du direkt loslegen.

---

## Erste Schritte

### Manuskript laden

1. Starte `ManuskriptHelfer.exe`.
2. Klick **„Manuskript öffnen …"**.
3. Wähle deine Datei. Unterstützt werden `.docx`, `.txt` und `.md`.

Während die Analyse läuft, wird der Button grau und der Cursor zur Sanduhr. Das Fenster bleibt bedienbar, du kannst dich schon umschauen.

### Was du in der Fußzeile siehst

Nach dem Laden steht unten in einer Zeile:

> 24 Kapitel · 87.532 Wörter · 292 Normseiten · ca. 438 Min. Lesezeit · 147 auffällige Wiederholungen · laut Datei: 288 Seiten

Der Zusatz **„laut Datei"** erscheint nur bei `.docx` und nur, wenn Word die Datei zuletzt gespeichert hat. Das ist die Seitenzahl, die Word unten links anzeigt – bei einem Normseiten-Layout sollte sie nah an der Normseiten-Zahl davor liegen.

---

## Die Tabs

### Statistik

Pro Kapitel: Wörter, Zeichen, Sätze, durchschnittliche Satzlänge, Lesezeit und Normseiten.

**Was du damit machst:** Wenn ein Kapitel deutlich mehr Wörter hat als die anderen, ist es entweder ein Handlungshöhepunkt oder ein Kapitel, das gestrafft werden kann. Das ist ein Hinweis, kein Urteil.

**Normseiten:** 30 Zeilen × 60 Zeichen = 1800 Zeichen. Wenn dein Word-Dokument im Normseiten-Raster formatiert ist, entspricht das der echten Seitenzahl. Für Verlags-Exposés ist das die Zahl, die du zitierst.

### Wiederholungen

Zeigt Wörter, die innerhalb von etwa einem Absatz (100 Wörter) mindestens dreimal vorkommen. Jeder Treffer hat eine Textstelle zum Nachlesen.

**Was du damit machst:**

- Zeile lesen, im Manuskript nachschauen, entscheiden: **umformulieren** oder **behalten**.
- Wenn behalten: Rechtsklick → **„Als geprüft markieren"**. Der Treffer verschwindet dauerhaft aus der Liste.
- Mehrere Zeilen gleichzeitig: `Strg` gedrückt halten und klicken, dann Rechtsklick.

Der Treffer bleibt in der Fußzeile gezählt, bis du das Manuskript neu lädst.

### Häufige Wörter

Pro Kapitel die 30 häufigsten Wörter mit mindestens 2 Vorkommen. Stoppwörter („und", „aber", „dass" usw.) und deine Figuren-/Ortsnamen sind schon rausgefiltert.

**Das ist der wichtigste Tab für Adjektiv-Wiederholungen.** Wenn dir beim Lesen auffällt, dass „kalt" oder „dunkel" zu oft kommt, steht es hier mit Zahl – und du kannst mit der Word-Suche oder im Wiederholungs-Tab zur Stelle springen.

**Warum nur Hinweise:** Die Liste sagt dir nicht, dass du umformulieren *musst*. Sie sagt dir, dass ein Wort Aufmerksamkeit verdient. Was du daraus machst, entscheidest du.

### Figuren

Zwei Bereiche untereinander:

**Oben: Auftritte pro Kapitel.** Zeilen = Kapitel, Spalten = deine Figuren/Orte, unten eine Gesamt-Zeile. Jede Zelle zeigt, wie oft der Name (inklusive Spitznamen) im Kapitel vorkommt.

Nutze das, um zu prüfen, ob eine Figur auftaucht, wo sie es sollte – oder ob sie in einem Kapitel verschwunden ist.

**Unten: Mögliche Schreibvarianten.** Wörter, die einem deiner Namen sehr ähnlich sehen – der klassische Tippfehler „Lysandar" statt „Lysander". Jeder Treffer hat eine Stelle zum Nachlesen.

**Was du damit machst:**

- Rechtsklick → **„Als geprüft markieren"**: Der Fund ist gewollt (z. B. eine andere Figur, die zufällig ähnlich heißt). Verschwindet aus der Liste.
- Rechtsklick → **„Als Alias übernehmen"**: Der Fund ist dieselbe Figur unter anderem Namen. Wird in `figuren.json` eingetragen und ab sofort als gültig gezählt. Die Variante verschwindet aus der Liste, und in der Auftritts-Matrix wird sie mitgezählt.

---

## Die Figurenliste pflegen

Die Figurenliste ist eine JSON-Datei. Klick im Figuren-Tab auf **„Figurenliste bearbeiten …"** – sie öffnet sich im Standard-Editor (meist Notepad oder VS Code).

Format:

```json
[
  {
    "Name": "Lysander Falk",
    "Aliases": ["Lysander", "der Falkner", "Ly"]
  },
  {
    "Name": "Burg Rosenstein",
    "Aliases": ["Rosenstein", "die Burg"]
  }
]