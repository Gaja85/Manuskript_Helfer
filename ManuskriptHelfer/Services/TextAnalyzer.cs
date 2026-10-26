using System.Text.RegularExpressions;
using ManuskriptHelfer.Models;

namespace ManuskriptHelfer.Services;

public static class TextAnalyzer
{
    private const double WordsPerMinute = 200;          // grobe Lesegeschwindigkeit
    private const double CharactersPerNormPage = 1800;  // 30 Zeilen × 60 Zeichen

    private static readonly Regex WordRegex = new(@"\p{L}[\p{L}'’\-]*", RegexOptions.Compiled);
    private static readonly Regex SentenceSplit = new(@"[.!?…]+", RegexOptions.Compiled);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "und","oder","aber","denn","weil","dass","wenn","als","wie","auch","noch","nur","schon",
        "sich","sein","seine","seiner","ihre","ihr","ihrer","mein","dein","eine","einer","einen",
        "einem","eines","dieser","diese","dieses","dann","doch","sind","war","waren","wird",
        "wurde","hatte","hatten","haben","nicht","kein","keine","über","unter","nach","vor",
        "bei","mit","von","aus","für","auf","durch","gegen","ohne","zum","zur","dem","den",
        "der","die","das","des","ein","ich","du","er","sie","es","wir","mir","dir",
        "ihm","ihn","uns","euch","hier","dort","jetzt","wieder","immer","sehr","mehr"
    };

    public static ChapterStats Analyze(Chapter chapter)
    {
        var words = WordRegex.Matches(chapter.Text).Count;
        var sentences = SentenceSplit
            .Split(chapter.Text)
            .Count(s => WordRegex.IsMatch(s));

        return new ChapterStats(
            chapter.Title,
            words,
            chapter.Text.Length,
            sentences,
            sentences == 0 ? 0 : (double)words / sentences,
            words / WordsPerMinute,
            chapter.Text.Length / CharactersPerNormPage);
    }

    public static List<RepetitionHit> FindRepetitions(
        Chapter chapter, int windowSize = 100, int minOccurrences = 3, int minLength = 4)
    {
        var words = WordRegex.Matches(chapter.Text).Select(m => m.Value).ToList();
        var positions = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < words.Count; i++)
        {
            var w = words[i];
            if (w.Length < minLength || StopWords.Contains(w)) continue;

            if (!positions.TryGetValue(w, out var list))
                positions[w] = list = new List<int>();
            list.Add(i);
        }

        var hits = new List<RepetitionHit>();
        foreach (var (word, list) in positions)
        {
            var i = 0;
            while (i + minOccurrences - 1 < list.Count)
            {
                var last = i + minOccurrences - 1;
                if (list[last] - list[i] <= windowSize)
                {
                    var end = last;
                    while (end + 1 < list.Count && list[end + 1] - list[i] <= windowSize) end++;

                    hits.Add(new RepetitionHit(
                        chapter.Title, word, end - i + 1, BuildContext(words, list[i])));
                    i = end + 1;
                }
                else
                {
                    i++;
                }
            }
        }

        return hits
            .OrderByDescending(h => h.Occurrences)
            .ThenBy(h => h.Word, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Häufigste Wörter pro Kapitel – reine Hinweisliste, was im Kapitel oft vorkommt.
    /// Stoppwörter und <paramref name="exclude"/> (z. B. Figuren-/Ortsnamen) werden
    /// ignoriert, Wörter unter <paramref name="minCount"/> auch.
    /// </summary>
    public static List<WordFrequencyHit> TopWordsPerChapter(
        List<Chapter> chapters,
        IReadOnlySet<string>? exclude = null,
        int topPerChapter = 30,
        int minCount = 2,
        int minLength = 4)
    {
        var result = new List<WordFrequencyHit>();

        foreach (var chapter in chapters)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (Match m in WordRegex.Matches(chapter.Text))
            {
                var w = m.Value;
                if (w.Length < minLength) continue;
                if (StopWords.Contains(w)) continue;
                if (exclude is not null && exclude.Contains(w)) continue;
                counts[w] = counts.GetValueOrDefault(w) + 1;
            }

            var top = counts
                .Where(kv => kv.Value >= minCount)
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Take(topPerChapter);

            foreach (var kv in top)
                result.Add(new WordFrequencyHit(chapter.Title, kv.Key, kv.Value));
        }

        return result;
    }

    private static string BuildContext(List<string> words, int index)
    {
        var from = Math.Max(0, index - 5);
        var to = Math.Min(words.Count, index + 10);
        return "… " + string.Join(' ', words.Skip(from).Take(to - from)) + " …";
    }
}