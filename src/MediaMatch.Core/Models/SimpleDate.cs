namespace MediaMatch.Core.Models;

public readonly record struct SimpleDate(int Year, int Month, int Day) : IComparable<SimpleDate>
{
    public DateOnly ToDateOnly() => new(Year, Month, Day);

    public static SimpleDate FromDateOnly(DateOnly date) => new(date.Year, date.Month, date.Day);

    public static SimpleDate? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (DateOnly.TryParse(text, out var date))
            return FromDateOnly(date);
        return null;
    }

    public int CompareTo(SimpleDate other)
    {
        var cmp = Year.CompareTo(other.Year);
        if (cmp != 0) return cmp;
        cmp = Month.CompareTo(other.Month);
        return cmp != 0 ? cmp : Day.CompareTo(other.Day);
    }

    public override string ToString() => $"{Year:D4}-{Month:D2}-{Day:D2}";
}
