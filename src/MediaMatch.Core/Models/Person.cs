namespace MediaMatch.Core.Models;

public sealed record Person(
    string Name,
    string? Character = null,
    string? Department = null,
    string? Job = null,
    int? TmdbId = null,
    string? ProfileUrl = null,
    int? Order = null);
