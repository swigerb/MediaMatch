namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a simple year-month-day date without time or timezone information.
/// </summary>
/// <param name="Year">The year component.</param>
/// <param name="Month">The month component (1–12).</param>
/// <param name="Day">The day component (1–31).</param>
public readonly record struct SimpleDate(int Year, int Month, int Day) : IComparable<SimpleDate>
{
    /// <summary>Converts this instance to a <see cref="DateOnly"/> value.</summary>
    /// <returns>A <see cref="DateOnly"/> representing the same date.</returns>
    public DateOnly ToDateOnly() => new(Year, Month, Day);

    /// <summary>Creates a <see cref="SimpleDate"/> from a <see cref="DateOnly"/> value.</summary>
    /// <param name="date">The source date.</param>
    /// <returns>A <see cref="SimpleDate"/> representing the same date.</returns>
    public static SimpleDate FromDateOnly(DateOnly date) => new(date.Year, date.Month, date.Day);

    /// <summary>Attempts to parse a date string into a <see cref="SimpleDate"/>.</summary>
    /// <param name="text">The date string to parse.</param>
    /// <returns>A <see cref="SimpleDate"/> if parsing succeeds; otherwise, <c>null</c>.</returns>
    public static SimpleDate? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (DateOnly.TryParse(text, out var date))
            return FromDateOnly(date);
        return null;
    }

    /// <inheritdoc />
    public int CompareTo(SimpleDate other)
    {
        var cmp = Year.CompareTo(other.Year);
        if (cmp != 0) return cmp;
        cmp = Month.CompareTo(other.Month);
        return cmp != 0 ? cmp : Day.CompareTo(other.Day);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Year:D4}-{Month:D2}-{Day:D2}";
}
