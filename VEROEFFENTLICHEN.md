# Veröffentlichen: Schritt für Schritt

Diese Anleitung führt dich von „alles liegt lokal" zu „auf GitHub veröffentlicht". Rechne mit 15–20 Minuten.

## Voraussetzungen

- [Git installiert](https://git-scm.com/download/win)
- GitHub-Account `Gaja85`
- Dein lauffähiges Projekt (VS-Build funktioniert)

## 1. Repository auf GitHub anlegen

1. Auf [github.com/new](https://github.com/new) gehen.
2. **Repository name:** `Manuskript_Helfer`
3. **Description:** „Analyse-Werkzeug für deutschsprachige Autorinnen und Autoren: Wortfrequenz, Wiederholungen, Normseiten, Figurenverwaltung – lokal, ohne Cloud."
4. **Public** auswählen.
5. **Keine** README, .gitignore oder Lizenz hinzufügen (die kommen aus dieser Sammlung).
6. „Create repository" klicken.

## 2. Dateien einsortieren

Dein Code (csproj, App.*, MainWindow.*, Models, Services) bleibt, wo er ist. Du legst die neuen Dateien dazu — Struktur siehe oben im Verzeichnisbaum.

## 3. Erstes Commit

Im Projektordner:

```bash
git init
git add .
git commit -m "Initial commit: Manuskript-Helfer 0.3.0"
git branch -M main
git remote add origin https://github.com/Gaja85/Manuskript_Helfer.git
git push -u origin main