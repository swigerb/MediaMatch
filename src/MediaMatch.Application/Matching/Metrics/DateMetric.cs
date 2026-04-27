using MediaMatch.Core.Matching;
using MediaMatch.Core.Models;

namespace MediaMatch.Application.Matching.Metrics;

public sealed class DateMetric : ISimilarityMetric
{
    public string Name => "Date";

    public float GetSimilarity(object? a, object? b)
    {
        if (!TryExtractDate(a, out var da) || !TryExtractDate(b, out var db))
            return 0.0f;

        var daysDiff = Math.Abs(da.DayNumber - db.DayNumber);

        if (daysDiff == 0)
            return 1.0f;

        // Score decays with distance: 1 / (1 + days/30)
        return (float)(1.0 / (1.0 + (double)daysDiff / 30.0));
    }

    private static bool TryExtractDate(object? value, out DateOnly date)
    {
        date = default;
        if (value is null) return false;

        if (value is SimpleDate sd)
        {
            date = sd.ToDateOnly();
            return true;
        }

        if (value is DateOnly d)
        {
            date = d;
            return true;
        }

        if (value is DateTime dt)
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }

        if (value is DateTimeOffset dto)
        {
            date = DateOnly.FromDateTime(dto.DateTime);
            return true;
        }

        var text = value.ToString();
        if (!string.IsNullOrWhiteSpace(text) && DateOnly.TryParse(text, out var parsed))
        {
            date = parsed;
            return true;
        }

        return false;
    }
}
