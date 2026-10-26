using System.Data;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using ManuskriptHelfer.Models;

namespace ManuskriptHelfer.Services;

public static class CharacterTracker
{
    private static readonly Regex WordRegex = new(@"\p{L}[\p{L}'’\-]*", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    private const string ExampleJson = """
        [
          { "Name": "Anna Beispiel", "Aliases": ["Anna", "die Archivarin"] },
          { "Name": "Burg Rosenstein", "Aliases": ["Rosenstein"] }
        ]
        """;

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ManuskriptHelfer", "figuren.json");

    public static void EnsureFileExists(string path)
    {
        if (File.Exists(path)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, ExampleJson);
    }

    public static List<Character> Load(string path)
    {
        EnsureFileExists(path);
        var list = JsonSerializer.Deserialize<List<Character>>(File.ReadAllText(path), JsonOptions)
                   ?? new List<Character>();
        return list.Where(c => !string.IsNullOrWhiteSpace(c.Name)).ToList();
    }

    public static void Save(string path, List<Character> characters)
    {
        var json = JsonSerializer.Serialize(characters, JsonOptions);
        File.WriteAllText(path, json);
    }

    // ---------- Auftrittsübersicht ----------

    public static DataTable BuildMatrix(List<Chapter> chapters, List<Character> characters)
    {
        var regexes = characters.Select(BuildRegex).ToList();

        var table = new DataTable();
        table.Columns.Add("Kapitel", typeof(string));
        for (var i = 0; i < characters.Count; i++)
            table.Columns.Add($"f{i}", typeof(int)).Caption = characters[i].Name;

        var totals = new int[characters.Count];
        foreach (var chapter in chapters)
        {
            var row = table.NewRow();
            row["Kapitel"] = chapter.Title;
            for (var i = 0; i < characters.Count; i++)
            {
                var count = regexes[i].Matches(chapter.Text).Count;
                row[$"f{i}"] = count;
                totals[i] += count;
            }
            table.Rows.Add(row);
        }

        var sum = table.NewRow();
        sum["Kapitel"] = "Gesamt";
        for (var i = 0; i < characters.Count; i++) sum[$"f{i}"] = totals[i];
        table.Rows.Add(sum);

        return table;
    }

    private static Regex BuildRegex(Character c)
    {
        var terms = c.Aliases.Prepend(c.Name)
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(t => t.Length)
            .Select(t => Regex.Escape(t).Replace("\\ ", @"\s+"));

        return new Regex(
            @"(?<!\p{L})(?:" + string.Join("|", terms) + @")(?:s|['’]s)?(?!\p{L})",
            RegexOptions.IgnoreCase);
    }

    // ---------- Schreibvarianten ----------

    public static List<NameVariantHit> FindVariants(List<Chapter> chapters, List<Character> characters)
    {
        var known = new Dictionary<string, string>();
        foreach (var term in characters.SelectMany(c => c.Aliases.Prepend(c.Name)))
        {
            foreach (var raw in term.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var part = raw.Trim(',', '.', ';', ':', '"', '„', '“');
                if (part.Length >= 4) known.TryAdd(part.ToLowerInvariant(), part);
            }
        }

        var buckets = known
            .GroupBy(kv => kv.Key[0])
            .ToDictionary(g => g.Key, g => g.Select(kv => (kv.Key, kv.Value)).ToList());

        var hits = new List<NameVariantHit>();
        foreach (var chapter in chapters)
        {
            var words = WordRegex.Matches(chapter.Text).Select(m => m.Value).ToList();
            var counts = new Dictionary<(string Found, string Similar), (int Count, int FirstPos)>();

            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                if (word.Length < 4 || !char.IsUpper(word[0])) continue;

                var lower = Regex.Replace(word.ToLowerInvariant(), @"['’]s$", "");
                if (IsKnown(lower, known)) continue;

                var similar = FindClosest(lower, buckets);
                if (similar is null) continue;

                var key = (word, similar);
                if (counts.TryGetValue(key, out var ex))
                    counts[key] = (ex.Count + 1, ex.FirstPos);
                else
                    counts[key] = (1, i);
            }

            hits.AddRange(counts.Select(kv =>
                new NameVariantHit(
                    chapter.Title, kv.Key.Found, kv.Key.Similar, kv.Value.Count,
                    BuildContext(words, kv.Value.FirstPos))));
        }

        return hits
            .OrderByDescending(h => h.Occurrences)
            .ThenBy(h => h.SimilarTo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsKnown(string lower, Dictionary<string, string> known) =>
        known.ContainsKey(lower) ||
        (lower.EndsWith('s') && known.ContainsKey(lower[..^1]));

    private static string? FindClosest(
        string lower,
        Dictionary<char, List<(string Key, string Original)>> buckets)
    {
        if (!buckets.TryGetValue(lower[0], out var candidates)) return null;

        string? best = null;
        var bestDistance = int.MaxValue;

        foreach (var (key, original) in candidates)
        {
            var maxDistance = key.Length >= 8 ? 2 : 1;
            if (Math.Abs(key.Length - lower.Length) > maxDistance + 1) continue;

            var distance = Levenshtein(lower, key);
            if (lower.EndsWith('s'))
                distance = Math.Min(distance, Levenshtein(lower[..^1], key));

            if (distance <= maxDistance && distance < bestDistance)
            {
                best = original;
                bestDistance = distance;
            }
        }
        return best;
    }

    private static string BuildContext(List<string> words, int index)
    {
        var from = Math.Max(0, index - 5);
        var to = Math.Min(words.Count, index + 10);
        return "… " + string.Join(' ', words.Skip(from).Take(to - from)) + " …";
    }

    private static int Levenshtein(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[b.Length];
    }
}