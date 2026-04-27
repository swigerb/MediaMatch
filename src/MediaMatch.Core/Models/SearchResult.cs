namespace MediaMatch.Core.Models;

public sealed record SearchResult(
    string Name,
    int Id,
    IReadOnlyList<string>? AliasNames = null)
{
    public override string ToString() => Name;
}
