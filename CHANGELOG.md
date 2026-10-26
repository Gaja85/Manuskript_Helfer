# Changelog

Alle nennenswerten Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
und dieses Projekt folgt [Semantic Versioning](https://semver.org/lang/de/).

## [Unreleased]

## [0.3.0] - 2026-10-26

### Hinzugefügt
- Tab „Häufige Wörter" mit den 30 häufigsten Wörtern pro Kapitel (Stoppwörter und Figuren-/Ortsnamen ausgefiltert)
- Normseiten-Berechnung (30 Zeilen × 60 Zeichen = 1800 Zeichen pro Seite) in der Statistik
- Word-eigene Seitenzahl aus den Dateieigenschaften („laut Datei: X Seiten")
- Persistente Prüfliste (`geprueft.json`) für abgehakte Wiederholungen und Schreibvarianten
- Rechtsklick-Kontextmenüs zum Markieren als „geprüft" (Mehrfachauswahl möglich)
- „Als Alias übernehmen" für Schreibvarianten schreibt in die `figuren.json`
- Review-Export als Markdown-Tabelle (Wiederholungen und Schreibvarianten)
- Kontextspalte bei Schreibvarianten

### Geändert
- Ladevorgang läuft asynchron, Fenster bleibt bedienbar
- Schreibvarianten-Suche nach Anfangsbuchstaben gebucketed (deutlich schneller bei großen Manuskripten)

## [0.2.0]

### Hinzugefügt
- Tab „Figuren" mit Auftritts-Matrix (Kapitel × Figur)
- Schreibvarianten-Erkennung über Levenshtein-Distanz
- Figurenliste als editierbare JSON-Datei (`figuren.json`)

## [0.1.0]

### Hinzugefügt
- Manuskript laden aus `.docx`, `.txt`, `.md`
- Kapitel-Erkennung über Überschriften und Schlüsselwörter
- Statistik-Tab (Wörter, Zeichen, Sätze, Satzlänge, Lesezeit)
- Wiederholungs-Erkennung innerhalb eines 100-Wörter-Fensters