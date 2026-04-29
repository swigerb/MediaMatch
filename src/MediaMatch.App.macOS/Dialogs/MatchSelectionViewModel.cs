using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaMatch.Core.Models;

namespace MediaMatch.App.macOS.Dialogs;

/// <summary>
/// ViewModel for the match selection dialog.
/// Displays ranked match candidates and captures the user's selection.
/// </summary>
public partial class MatchSelectionViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial MatchSuggestion? SelectedMatch { get; set; }

    /// <summary>
    /// Ranked match candidates from opportunistic matching.
    /// </summary>
    public ObservableCollection<MatchSuggestionItem> Suggestions { get; } = [];

    /// <summary>
    /// Loads match suggestions into the observable collection.
    /// </summary>
    public void LoadSuggestions(string fileName, IEnumerable<MatchSuggestion> suggestions)
    {
        FileName = fileName;
        Suggestions.Clear();

        foreach (var suggestion in suggestions)
        {
            Suggestions.Add(new MatchSuggestionItem(suggestion));
        }
    }
}

/// <summary>
/// Wraps a <see cref="MatchSuggestion"/> for UI binding with formatted properties.
/// </summary>
public partial class MatchSuggestionItem : ObservableObject
{
    public MatchSuggestion Suggestion { get; }

    public string Title => Suggestion.Year.HasValue
        ? $"{Suggestion.Title} ({Suggestion.Year})"
        : Suggestion.Title;

    public string ProviderName => Suggestion.ProviderName;
    public double ConfidencePercent => Suggestion.Confidence * 100;
    public string ConfidenceText => $"{ConfidencePercent:F0}%";
    public string Description => Suggestion.MetadataSummary ?? string.Empty;

    /// <summary>
    /// Artwork URL from the provider, if available.
    /// </summary>
    public string? ArtworkUrl { get; set; }

    public MatchSuggestionItem(MatchSuggestion suggestion)
    {
        Suggestion = suggestion;
    }
}
