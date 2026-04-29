namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a simple year-month-day date without time or timezone information.
/// </summary>
/// <param name="Year">The year component.</param>
/// <param name="Month">The month component (1–12).</param>
/// <param name="Day">The day component (1–31).</param>
public readonly record struct SimpleDate(int Year, int Month, int Day) : IComparable<SimpleDate>
{
    /// <summary>
    /// Returns <c>true</c> if this instance represents a valid Gregorian calendar date.
    /// The default value (Year=0, Month=0, Day=0) is not valid.
    /// </summary>
    public bool IsValid =>
        Year is >= 1 and <= 9999 &&
        Month is >= 1 and <= 12 &&
        Day >= 1 && Day <= DateTime.DaysInMonth(Year, Month);

    /// <summary>Converts this instance to a <see cref="DateOnly"/> value if valid.</summary>
    /// <returns>
    /// A <see cref="DateOnly"/> representing the same date, or <c>null</c> if this
    /// instance is the default value or otherwise represents an invalid date.
    /// </returns>
    public DateOnly? ToDateOnly() => IsValid ? new DateOnly(Year, Month, Day) : null;

    /// <summary>Creates a <see cref="SimpleDate"/> from a <see cref="DateOnly"/> value.</summary>
    /// <param name="date">The source date.</param>
    /// <returns>A <see cref="SimpleDate"/> representing the same date.</returns>
    public static SimpleDate FromDateOnly(DateOnly date) => new(date.Year, date.Month, date.Day);

    /// <summary>
    /// Attempts to construct a <see cref="SimpleDate"/> from year/month/day components,
    /// validating that they form a real calendar date.
    /// </summary>
    /// <param name="year">The year (1–9999).</param>
    /// <param name="month">The month (1–12).</param>
    /// <param name="day">The day of month (1–end of month).</param>
    /// <param name="result">The created <see cref="SimpleDate"/> when successful.</param>
    /// <returns><c>true</c> if the components form a valid date; otherwise, <c>false</c>.</returns>
    public static bool TryCreate(int year, int month, int day, out SimpleDate result)
    {
        var candidate = new SimpleDate(year, month, day);
        if (candidate.IsValid)
        {
            result = candidate;
            return true;
        }

        result = default;
        return false;
    }

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
