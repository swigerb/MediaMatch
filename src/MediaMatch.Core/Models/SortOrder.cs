namespace MediaMatch.Core.Models;

/// <summary>
/// Specifies the episode ordering scheme used by a metadata provider.
/// </summary>
public enum SortOrder
{
    /// <summary>Episodes ordered by original air date.</summary>
    Airdate,

    /// <summary>Episodes ordered by DVD release sequence.</summary>
    DvdOrder,

    /// <summary>Episodes ordered by absolute number across all seasons.</summary>
    AbsoluteNumber
}
