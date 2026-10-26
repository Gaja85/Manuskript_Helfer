using System.IO;
using System.Text;
using ManuskriptHelfer.Models;

namespace ManuskriptHelfer.Services;

/// <summary>
/// Schreibt die Review-Listen (Wiederholungen, Schreibvarianten) als Markdown,
/// damit sie sich in Obsidian, VS Code o. ä. direkt abarbeiten lassen.
/// </summary>
public static class ReviewExporter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static void Export(
        string path,
        string manuscriptName,
        IReadOnlyList<ChapterStats> stats,
        IReadOnlyList<RepetitionHit> repetitions,
        IReadOnlyList<NameVariantHit> variants)
    {
        var totalWords = stats.Sum(s => s.Words);
        var totalPages = stats.Sum(s => s.NormPages);

        var sb = new StringBuilder();
        sb.AppendLine($"# Review: {manuscriptName}");
        sb.AppendLine();
        sb.AppendLine($"_Erstellt am {DateTime.Now:dd.MM.yyyy HH:mm} · " +
                      $"{stats.Count} Kapitel · {totalWords:N0} Wörter · {totalPages:N0} Normseiten_");
        sb.AppendLine();

        sb.AppendLine($"## Auffällige Wiederholungen ({repetitions.Count})");
        sb.AppendLine();
        if (repetitions.Count == 0)
        {
            sb.AppendLine("_Keine._");
        }
        else
        {
            sb.AppendLine("| Kapitel | Wort | Anzahl | Stelle |");
            sb.AppendLine("|---|---|---:|---|");
            foreach (var h in repetitions)
                sb.AppendLine($"| {Esc(h.Chapter)} | {Esc(h.Word)} | {h.Occurrences} | {Esc(h.Context)} |");
        }
        sb.AppendLine();

        sb.AppendLine($"## Mögliche Schreibvarianten ({variants.Count})");
        sb.AppendLine();
        if (variants.Count == 0)
        {
            sb.AppendLine("_Keine._");
        }
        else
        {
            sb.AppendLine("| Kapitel | Gefunden | Ähnlich zu | Anzahl | Stelle |");
            sb.AppendLine("|---|---|---|---:|---|");
            foreach (var h in variants)
                sb.AppendLine($"| {Esc(h.Chapter)} | {Esc(h.Found)} | {Esc(h.SimilarTo)} | {h.Occurrences} | {Esc(h.Context)} |");
        }

        File.WriteAllText(path, sb.ToString(), Utf8NoBom);
    }

    private static string Esc(string s) =>
        s.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
}