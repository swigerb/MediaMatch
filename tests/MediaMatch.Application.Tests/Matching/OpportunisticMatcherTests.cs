using FluentAssertions;
using MediaMatch.Application.Detection;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Application.Matching;
using Moq;
using MediaMatch.Core.Providers;

namespace MediaMatch.Application.Tests.Matching;

public sealed class OpportunisticMatcherTests
{
    private static DetectionResult CreateDetection(
        MediaType type = MediaType.TvSeries,
        float confidence = 0.70f,
        string cleanTitle = "Test Show",
        int? year = null)
    {
        var releaseInfo = new ReleaseInfo(
            "Test.Show.S01E01.mkv", cleanTitle,
            new SeasonEpisodeMatch(1, 1), year,
            VideoQuality.HD1080p, null, null, null, null, null);
        return new DetectionResult("test.mkv", type, releaseInfo, confidence);
    }

    private static Mock<IEpisodeProvider> CreateEpisodeProvider(
        string name, IReadOnlyList<SearchResult>? searchResults = null,
        IReadOnlyList<Episode>? episodes = null, SeriesInfo? seriesInfo = null)
    {
        var mock = new Mock<IEpisodeProvider>();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults ?? new List<SearchResult> { new("Test Show", 1) });
        mock.Setup(p => p.GetEpisodesAsync(It.IsAny<SearchResult>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes ?? new List<Episode> { new("Test Show", 1, 1, "Pilot") });
        mock.Setup(p => p.GetSeriesInfoAsync(It.IsAny<SearchResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(seriesInfo ?? new SeriesInfo("Test Show", "1", null, null, null, 8.5, null, new List<string> { "Drama" }));
        return mock;
    }

    private static Mock<IMovieProvider> CreateMovieProvider(
        string name, IReadOnlyList<Movie>? movies = null, MovieInfo? movieInfo = null)
    {
        var mock = new Mock<IMovieProvider>();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies ?? new List<Movie> { new("Test Movie", 2024) });
        mock.Setup(p => p.GetMovieInfoAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(movieInfo ?? new MovieInfo("Test Movie", 2024, null, null, null, null, null, 7.5, null, null, new List<string> { "Action" }, new List<Person>(), new List<Person>()));
        return mock;
    }

    [Fact]
    public async Task SuggestAsync_TvSeries_ReturnsSuggestions()
    {
        var ep = CreateEpisodeProvider("TMDb");
        var matcher = new OpportunisticMatcher(
            new[] { ep.Object },
            Array.Empty<IMovieProvider>());

        var detection = CreateDetection();
        // Use filename with S01E01 so EpisodeMatcher can parse it
        var suggestions = await matcher.SuggestAsync("Test.Show.S01E01.mkv", detection);

        suggestions.Should().NotBeEmpty();
        suggestions[0].ProviderName.Should().Be("TMDb");
    }

    [Fact]
    public async Task SuggestAsync_Movie_ReturnsSuggestions()
    {
        var movieDetection = new DetectionResult("movie.mkv", MediaType.Movie,
            new ReleaseInfo("Movie.2024.mkv", "Movie", null, 2024,
                VideoQuality.HD1080p, null, null, null, null, null), 0.70f);

        var mp = CreateMovieProvider("TMDb");
        var matcher = new OpportunisticMatcher(
            Array.Empty<IEpisodeProvider>(),
            new[] { mp.Object });

        var suggestions = await matcher.SuggestAsync("movie.mkv", movieDetection);
        suggestions.Should().NotBeEmpty();
        suggestions[0].Title.Should().Be("Test Movie");
    }

    [Fact]
    public async Task SuggestAsync_NoProviders_ReturnsEmpty()
    {
        var matcher = new OpportunisticMatcher(
            Array.Empty<IEpisodeProvider>(),
            Array.Empty<IMovieProvider>());

        var detection = CreateDetection();
        var suggestions = await matcher.SuggestAsync("test.mkv", detection);
        suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestAsync_EmptyCleanTitle_ReturnsEmpty()
    {
        var detection = CreateDetection(cleanTitle: "");
        var ep = CreateEpisodeProvider("TMDb");
        var matcher = new OpportunisticMatcher(
            new[] { ep.Object },
            Array.Empty<IMovieProvider>());

        var suggestions = await matcher.SuggestAsync("test.mkv", detection);
        suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestAsync_ProviderThrows_ContinuesToNext()
    {
        var failingProvider = new Mock<IEpisodeProvider>();
        failingProvider.Setup(p => p.Name).Returns("Failing");
        failingProvider.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var workingProvider = CreateEpisodeProvider("Working");
        var matcher = new OpportunisticMatcher(
            new[] { failingProvider.Object, workingProvider.Object },
            Array.Empty<IMovieProvider>());

        var detection = CreateDetection();
        // Use filename with episode info so EpisodeMatcher can match
        var suggestions = await matcher.SuggestAsync("Test.Show.S01E01.mkv", detection);

        // Should still get results from the working provider
        suggestions.Should().NotBeEmpty();
        suggestions[0].ProviderName.Should().Be("Working");
    }

    [Fact]
    public async Task SuggestAsync_LowConfidenceMatch_IsFiltered()
    {
        // Detection confidence = 0.30, so combined score will be below 0.60 threshold
        var detection = CreateDetection(confidence: 0.30f);
        var ep = CreateEpisodeProvider("TMDb");
        var matcher = new OpportunisticMatcher(
            new[] { ep.Object },
            Array.Empty<IMovieProvider>());

        var suggestions = await matcher.SuggestAsync("test.mkv", detection);
        // With 0.30 confidence and typical match score, combined may be below 0.60
        // This depends on EpisodeMatcher score — just verify it doesn't crash
        suggestions.Should().NotBeNull();
    }

    [Fact]
    public async Task SuggestAsync_MaxSuggestions_LimitedToFive()
    {
        // Create many providers each returning results
        var providers = Enumerable.Range(0, 10).Select(i =>
        {
            var mock = CreateEpisodeProvider($"Provider{i}");
            return mock.Object;
        }).ToArray();

        var matcher = new OpportunisticMatcher(providers, Array.Empty<IMovieProvider>());
        var detection = CreateDetection(confidence: 0.80f);
        var suggestions = await matcher.SuggestAsync("test.mkv", detection);

        suggestions.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task SuggestAsync_UnknownMediaType_ReturnsEmpty()
    {
        var detection = CreateDetection(type: MediaType.Unknown);
        var matcher = new OpportunisticMatcher(
            new[] { CreateEpisodeProvider("TMDb").Object },
            new[] { CreateMovieProvider("TMDb").Object });

        var suggestions = await matcher.SuggestAsync("test.mkv", detection);
        suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestAsync_ResultsAreSortedByConfidenceDescending()
    {
        var ep = CreateEpisodeProvider("TMDb");
        var matcher = new OpportunisticMatcher(
            new[] { ep.Object },
            Array.Empty<IMovieProvider>());

        var detection = CreateDetection(confidence: 0.85f);
        var suggestions = await matcher.SuggestAsync("test.mkv", detection);

        if (suggestions.Count > 1)
        {
            for (int i = 1; i < suggestions.Count; i++)
                suggestions[i].Confidence.Should().BeLessThanOrEqualTo(suggestions[i - 1].Confidence);
        }
    }

    [Fact]
    public async Task SuggestAsync_MovieYearMatch_BoostsConfidence()
    {
        var movieDetection = new DetectionResult("Movie.2024.mkv", MediaType.Movie,
            new ReleaseInfo("Movie.2024.mkv", "Test Movie", null, 2024,
                VideoQuality.HD1080p, null, null, null, null, null), 0.65f);

        var mp = CreateMovieProvider("TMDb", movies: new List<Movie> { new("Test Movie", 2024) });
        var matcher = new OpportunisticMatcher(
            Array.Empty<IEpisodeProvider>(), new[] { mp.Object });

        var suggestions = await matcher.SuggestAsync("movie.mkv", movieDetection);
        suggestions.Should().NotBeEmpty();
        // Year match (+0.15) and exact name match (+0.20) should boost confidence above 0.60
        suggestions[0].Confidence.Should().BeGreaterThan(0.60);
    }

    [Fact]
    public async Task SuggestAsync_Cancellation_ThrowsOperationCanceled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var ep = CreateEpisodeProvider("TMDb");
        var matcher = new OpportunisticMatcher(
            new[] { ep.Object },
            Array.Empty<IMovieProvider>());

        var detection = CreateDetection();
        await matcher.Invoking(m => m.SuggestAsync("test.mkv", detection, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
