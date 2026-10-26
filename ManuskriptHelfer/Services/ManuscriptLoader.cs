using System.IO;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using ManuskriptHelfer.Models;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace ManuskriptHelfer.Services;

/// <summary>
/// Liest .docx, .txt und .md und teilt den Text in Kapitel.
/// Kapitelgrenzen: Überschrift 1 / Titel in Word, "# Überschrift" in Markdown
/// oder Zeilen, die mit "Kapitel", "Prolog", "Epilog" usw. beginnen.
/// </summary>
public static class ManuscriptLoader
{
    private sealed record Line(string Text, bool IsHeading);

    private static readonly Regex ChapterWord = new(
        @"^\s*(Kapitel|Chapter|Prolog|Epilog|Vorwort|Nachwort)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Word-Stil-IDs sind je nach Sprache unterschiedlich ("Heading1", "berschrift1", ...)
    private static readonly Regex Heading1Style = new(
        @"^(Heading ?1|berschrift ?1|Title|Titel)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static List<Chapter> Load(string path)
    {
        var lines = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".docx" => ReadDocx(path),
            ".txt" or ".md" => ReadText(path),
            var ext => throw new NotSupportedException($"Dateityp '{ext}' wird nicht unterstützt.")
        };

        return SplitIntoChapters(lines, Path.GetFileNameWithoutExtension(path));
    }

    /// <summary>
    /// Liest die von Word gespeicherte Seitenzahl aus den Dateieigenschaften.
    /// Nur für .docx, und nur, wenn Word die Datei zuletzt gespeichert hat –
    /// sonst null. Das ist die Zahl, die Word unten links anzeigt.
    /// </summary>
    public static int? TryGetPageCount(string path)
    {
        if (!path.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)) return null;
        try
        {
            using var doc = WordprocessingDocument.Open(path, false);
            var text = doc.ExtendedFilePropertiesPart?.Properties?.Pages?.Text;
            return int.TryParse(text, out var pages) && pages > 0 ? pages : null;
        }
        catch
        {
            return null;
        }
    }

    private static List<Line> ReadDocx(string path)
    {
        var result = new List<Line>();
        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return result;

        foreach (var p in body.Descendants<W.Paragraph>())
        {
            var text = p.InnerText.Trim();
            if (text.Length == 0) continue;

            var styleId = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";
            var isHeading = Heading1Style.IsMatch(styleId) || ChapterWord.IsMatch(text);
            result.Add(new Line(text, isHeading));
        }
        return result;
    }

    private static List<Line> ReadText(string path)
    {
        var result = new List<Line>();
        foreach (var raw in File.ReadLines(path))
        {
            var text = raw.Trim();
            if (text.Length == 0) continue;

            if (text.StartsWith('#'))
                result.Add(new Line(text.TrimStart('#', ' '), true));
            else
                result.Add(new Line(text, ChapterWord.IsMatch(text) && text.Length < 60));
        }
        return result;
    }

    private static List<Chapter> SplitIntoChapters(List<Line> lines, string fallbackTitle)
    {
        var chapters = new List<Chapter>();
        string? title = null;
        var buffer = new List<string>();

        void Flush()
        {
            if (buffer.Count == 0 && title is null) return;
            chapters.Add(new Chapter(title ?? "Vorspann", string.Join("\n", buffer)));
            buffer.Clear();
        }

        foreach (var line in lines)
        {
            if (line.IsHeading)
            {
                Flush();
                title = line.Text;
            }
            else
            {
                buffer.Add(line.Text);
            }
        }
        Flush();

        if (chapters.Count == 0)
            chapters.Add(new Chapter(fallbackTitle, string.Join("\n", lines.Select(l => l.Text))));

        return chapters;
    }
}