namespace ManuskriptHelfer.Models;

/// <summary>Ein Kapitel des Manuskripts (Titel + reiner Text).</summary>
public record Chapter(string Title, string Text);

/// <summary>Kennzahlen pro Kapitel.</summary>
public record ChapterStats(
    string Title,
    int Words,
    int Characters,
    int Sentences,
    double AvgSentenceLength,
    double ReadingMinutes,
    double NormPages);

/// <summary>Ein Wort, das innerhalb eines kurzen Textfensters auffällig oft vorkommt.</summary>
public record RepetitionHit(
    string Chapter,
    string Word,
    int Occurrences,
    string Context);

/// <summary>Eine Figur oder ein Ort aus der figuren.json.</summary>
public class Character
{
    public string Name { get; set; } = "";
    public List<string> Aliases { get; set; } = new();
}

/// <summary>Ein Wort, das einem bekannten Namen verdächtig ähnlich sieht.</summary>
public record NameVariantHit(
    string Chapter,
    string Found,
    string SimilarTo,
    int Occurrences,
    string Context);

/// <summary>Ein als "geprüft" markierter Wiederholungstreffer (Kapitel + Wort).</summary>
public record ReviewedRepetition(string Chapter, string Word);

/// <summary>Eine als "geprüft" markierte Schreibvariante (Kapitel + Fund + ÄhnlichZu).</summary>
public record ReviewedVariant(string Chapter, string Found, string SimilarTo);

/// <summary>Persistente Prüfliste: alles, was der Nutzer als ok/gewollt markiert hat.</summary>
public class ReviewState
{
    public List<ReviewedRepetition> Repetitions { get; set; } = new();
    public List<ReviewedVariant> Variants { get; set; } = new();
}

/// <summary>Ein häufiges Wort in einem Kapitel (Hinweis, keine Wertung).</summary>
public record WordFrequencyHit(string Chapter, string Word, int Count);