using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ManuskriptHelfer.Models;
using ManuskriptHelfer.Services;
using Microsoft.Win32;

namespace ManuskriptHelfer;

public partial class MainWindow : Window
{
    private List<Chapter> _chapters = new();
    private List<ChapterStats>? _stats;
    private List<RepetitionHit>? _allRepetitions;
    private List<NameVariantHit>? _allVariants;
    private List<RepetitionHit>? _repetitions;
    private List<NameVariantHit>? _variants;
    private DataTable? _matrix;
    private int? _documentPageCount;
    private string _manuscriptName = "";
    private ReviewState _reviewState = new();
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;

        var dialog = new OpenFileDialog
        {
            Title = "Manuskript auswählen",
            Filter = "Manuskripte (*.docx;*.txt;*.md)|*.docx;*.txt;*.md|Alle Dateien (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            SetBusy(true);
            FileLabel.Text = dialog.FileName;
            _manuscriptName = Path.GetFileNameWithoutExtension(dialog.FileName);

            var (chapters, stats, repetitions, frequency) = await Task.Run(() =>
            {
                var loaded = ManuscriptLoader.Load(dialog.FileName);
                var s = loaded.Select(TextAnalyzer.Analyze).ToList();
                var r = loaded.SelectMany(c => TextAnalyzer.FindRepetitions(c)).ToList();

                var chars = CharacterTracker.Load(CharacterTracker.DefaultPath);
                var excl = BuildExclusionSet(chars);
                var freq = TextAnalyzer.TopWordsPerChapter(loaded, excl);

                return (loaded, s, r, freq);
            });

            _documentPageCount = ManuscriptLoader.TryGetPageCount(dialog.FileName);
            _reviewState = ReviewStateStore.Load(ReviewStateStore.DefaultPath);
            _chapters = chapters;
            _stats = stats;
            _allRepetitions = repetitions;

            StatsGrid.ItemsSource = stats;
            FrequencyGrid.ItemsSource = frequency;
            ApplyReviewFilter();
            UpdateSummary();

            await RunCharacterCheckAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler beim Laden",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static HashSet<string> BuildExclusionSet(List<Character> characters)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in characters)
        {
            set.Add(c.Name);
            foreach (var part in c.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (part.Length >= 4) set.Add(part);
            foreach (var alias in c.Aliases)
            {
                set.Add(alias);
                foreach (var part in alias.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    if (part.Length >= 4) set.Add(part);
            }
        }
        return set;
    }

    private async Task RunCharacterCheckAsync()
    {
        if (_chapters.Count == 0)
        {
            CharacterInfo.Text = "Erst ein Manuskript öffnen.";
            return;
        }

        try
        {
            var (matrix, allVariants, charCount) = await Task.Run(() =>
            {
                var chars = CharacterTracker.Load(CharacterTracker.DefaultPath);
                var m = CharacterTracker.BuildMatrix(_chapters, chars);
                var v = CharacterTracker.FindVariants(_chapters, chars);
                return (m, v, chars.Count);
            });

            _matrix = matrix;
            _allVariants = allVariants;
            MatrixGrid.ItemsSource = _matrix.DefaultView;

            ApplyReviewFilter();
            CharacterInfo.Text = $"{charCount} Figuren/Orte · {_variants?.Count ?? 0} mögliche Schreibvarianten";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Die Figurenliste konnte nicht gelesen werden:\n" + ex.Message,
                "Figurenliste", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplyReviewFilter()
    {
        if (_allRepetitions is not null)
        {
            var ignoredReps = _reviewState.Repetitions
                .Select(r => (r.Chapter, r.Word))
                .ToHashSet();

            _repetitions = _allRepetitions
                .Where(h => !ignoredReps.Contains((h.Chapter, h.Word)))
                .ToList();
            RepetitionGrid.ItemsSource = _repetitions;
        }

        if (_allVariants is not null)
        {
            var ignoredVars = _reviewState.Variants
                .Select(v => (v.Chapter, v.Found, v.SimilarTo))
                .ToHashSet();

            _variants = _allVariants
                .Where(h => !ignoredVars.Contains((h.Chapter, h.Found, h.SimilarTo)))
                .ToList();
            VariantGrid.ItemsSource = _variants;

            if (CharacterInfo is not null && _allVariants is not null)
                CharacterInfo.Text = $"{_variants.Count} mögliche Schreibvarianten (nach Filter)";
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (_stats is null || _repetitions is null) return;
        var totalWords = _stats.Sum(s => s.Words);
        var totalMinutes = _stats.Sum(s => s.ReadingMinutes);
        var totalPages = _stats.Sum(s => s.NormPages);
        var summary =
            $"{_chapters.Count} Kapitel · {totalWords:N0} Wörter · " +
            $"{totalPages:N0} Normseiten · " +
            $"ca. {totalMinutes:N0} Min. Lesezeit · " +
            $"{_repetitions.Count} auffällige Wiederholungen";
        if (_documentPageCount is int docPages)
            summary += $" · laut Datei: {docPages} Seiten";
        SummaryLabel.Text = summary;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        OpenButton.IsEnabled = !busy;
        ExportButton.IsEnabled = !busy && _stats is not null;
        Cursor = busy ? Cursors.Wait : null;
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_stats is null || _repetitions is null || _variants is null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Review exportieren",
            Filter = "Markdown (*.md)|*.md|Alle Dateien (*.*)|*.*",
            FileName = $"{_manuscriptName}_review.md"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            ReviewExporter.Export(
                dialog.FileName, _manuscriptName, _stats, _repetitions, _variants);
            MessageBox.Show($"Export geschrieben:\n{dialog.FileName}",
                "Review-Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler beim Export",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ManageReviewed_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ReviewStateStore.EnsureFileExists(ReviewStateStore.DefaultPath);
            Process.Start(new ProcessStartInfo(ReviewStateStore.DefaultPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Datei konnte nicht geöffnet werden:\n{ReviewStateStore.DefaultPath}\n\n{ex.Message}",
                "Geprüfte verwalten", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MarkRepetitionsReviewed_Click(object sender, RoutedEventArgs e)
    {
        var selected = RepetitionGrid.SelectedItems.OfType<RepetitionHit>().ToList();
        if (selected.Count == 0) return;

        var already = _reviewState.Repetitions
            .Select(r => (r.Chapter, r.Word))
            .ToHashSet();

        var added = 0;
        foreach (var hit in selected)
        {
            if (already.Contains((hit.Chapter, hit.Word))) continue;
            _reviewState.Repetitions.Add(new ReviewedRepetition(hit.Chapter, hit.Word));
            added++;
        }

        if (added == 0) return;
        ReviewStateStore.Save(ReviewStateStore.DefaultPath, _reviewState);
        ApplyReviewFilter();
    }

    private void MarkVariantsReviewed_Click(object sender, RoutedEventArgs e)
    {
        var selected = VariantGrid.SelectedItems.OfType<NameVariantHit>().ToList();
        if (selected.Count == 0) return;

        var already = _reviewState.Variants
            .Select(v => (v.Chapter, v.Found, v.SimilarTo))
            .ToHashSet();

        var added = 0;
        foreach (var hit in selected)
        {
            if (already.Contains((hit.Chapter, hit.Found, hit.SimilarTo))) continue;
            _reviewState.Variants.Add(new ReviewedVariant(hit.Chapter, hit.Found, hit.SimilarTo));
            added++;
        }

        if (added == 0) return;
        ReviewStateStore.Save(ReviewStateStore.DefaultPath, _reviewState);
        ApplyReviewFilter();
    }

    private async void AddVariantAsAlias_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        var selected = VariantGrid.SelectedItems.OfType<NameVariantHit>().ToList();
        if (selected.Count == 0) return;

        try
        {
            SetBusy(true);
            var characters = CharacterTracker.Load(CharacterTracker.DefaultPath);
            var added = 0;
            var alreadyPresent = 0;
            var notFound = new List<string>();

            foreach (var hit in selected)
            {
                var target = characters.FirstOrDefault(c =>
                    string.Equals(c.Name, hit.SimilarTo, StringComparison.OrdinalIgnoreCase) ||
                    c.Aliases.Any(a => string.Equals(a, hit.SimilarTo, StringComparison.OrdinalIgnoreCase)));

                if (target is null)
                {
                    notFound.Add(hit.SimilarTo);
                    continue;
                }

                if (target.Aliases.Any(a => string.Equals(a, hit.Found, StringComparison.OrdinalIgnoreCase)))
                {
                    alreadyPresent++;
                    continue;
                }

                target.Aliases.Add(hit.Found);
                added++;
            }

            if (added > 0)
                CharacterTracker.Save(CharacterTracker.DefaultPath, characters);

            var msg = $"{added} Alias(e) in figuren.json ergänzt.";
            if (alreadyPresent > 0) msg += $"\n{alreadyPresent} bereits vorhanden.";
            if (notFound.Count > 0)
                msg += $"\nNicht in figuren.json gefunden: {string.Join(", ", notFound.Distinct())}";
            MessageBox.Show(msg, "Als Alias übernehmen",
                MessageBoxButton.OK, MessageBoxImage.Information);

            if (added > 0)
                await RunCharacterCheckAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler beim Übernehmen",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void GridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row && !row.IsSelected)
        {
            if (ItemsControl.ItemsControlFromItemContainer(row) is DataGrid grid)
                grid.SelectedItems.Clear();
            row.IsSelected = true;
        }
    }

    private void EditCharacters_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CharacterTracker.EnsureFileExists(CharacterTracker.DefaultPath);
            Process.Start(new ProcessStartInfo(CharacterTracker.DefaultPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Datei konnte nicht geöffnet werden:\n{CharacterTracker.DefaultPath}\n\n{ex.Message}",
                "Figurenliste", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Recheck_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        try
        {
            SetBusy(true);
            await RunCharacterCheckAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void MatrixGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (_matrix?.Columns[e.PropertyName] is { } column)
            e.Column.Header = column.Caption;
    }
}