namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a series search result returned by a metadata provider.
/// </summary>
/// <param name="Name">The series title.</param>
/// <param name="Id">The provider-specific series identifier.</param>
/// <param name="AliasNames">Alternative titles or aliases for the series.</param>
public sealed record SearchResult(
    string Name,
    int Id,
    IReadOnlyList<string>? AliasNames = null)
{
    /// <inheritdoc />
    public override string ToString() => Name;
}
