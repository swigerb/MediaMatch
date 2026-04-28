using FluentAssertions;
using MediaMatch.Core.Models;

namespace MediaMatch.Core.Tests.Models;

public sealed class SimpleDateTests
{
    [Fact]
    public void FromDateOnly_ConvertsCorrectly()
    {
        var dateOnly = new DateOnly(2023, 7, 4);

        var simple = SimpleDate.FromDateOnly(dateOnly);

        simple.Year.Should().Be(2023);
        simple.Month.Should().Be(7);
        simple.Day.Should().Be(4);
    }

    [Fact]
    public void TryParse_Iso8601_Parses()
    {
        var result = SimpleDate.TryParse("2024-01-15");

        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2024);
        result.Value.Month.Should().Be(1);
        result.Value.Day.Should().Be(15);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-date")]
    [InlineData("abc123")]
    [InlineData("2024-13-45")]
    public void TryParse_InvalidInput_ReturnsNull(string input)
    {
        SimpleDate.TryParse(input).Should().BeNull();
    }

    [Fact]
    public void CompareTo_SameDate_ReturnsZero()
    {
        var date = new SimpleDate(2023, 6, 15);

        date.CompareTo(date).Should().Be(0);
    }

    [Fact]
    public void CompareTo_EarlierDate_ReturnsNegative()
    {
        var earlier = new SimpleDate(2020, 3, 10);
        var later = new SimpleDate(2023, 6, 15);

        earlier.CompareTo(later).Should().BeNegative();
    }

    [Fact]
    public void CompareTo_LaterDate_ReturnsPositive()
    {
        var earlier = new SimpleDate(2020, 3, 10);
        var later = new SimpleDate(2023, 6, 15);

        later.CompareTo(earlier).Should().BePositive();
    }

    [Fact]
    public void CompareTo_SameYearDifferentMonth_ComparesMonth()
    {
        var jan = new SimpleDate(2023, 1, 15);
        var jun = new SimpleDate(2023, 6, 15);

        jan.CompareTo(jun).Should().BeNegative();
        jun.CompareTo(jan).Should().BePositive();
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var date = new SimpleDate(2023, 1, 5);

        date.ToString().Should().Be("2023-01-05");
    }
}
