using System.IO;
using System.Text.Json;
using ManuskriptHelfer.Models;

namespace ManuskriptHelfer.Services;

/// <summary>
/// Persistente Prüfliste: alles, was der Nutzer als "geprüft/gewollt" markiert
/// oder als Alias in die figuren.json übernommen hat, bleibt hier erhalten.
/// </summary>
public static class ReviewStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ManuskriptHelfer", "geprueft.json");

    public static void EnsureFileExists(string path)
    {
        if (File.Exists(path)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new ReviewState(), JsonOptions));
    }

    public static ReviewState Load(string path)
    {
        EnsureFileExists(path);
        try
        {
            return JsonSerializer.Deserialize<ReviewState>(File.ReadAllText(path), JsonOptions)
                   ?? new ReviewState();
        }
        catch
        {
            return new ReviewState();
        }
    }

    public static void Save(string path, ReviewState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(state, JsonOptions));
    }
}