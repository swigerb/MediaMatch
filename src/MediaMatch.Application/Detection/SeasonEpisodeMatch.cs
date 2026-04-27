namespace MediaMatch.Application.Detection;

public sealed record SeasonEpisodeMatch(
    int Season,
    int Episode,
    int? EndEpisode = null,
    int? AbsoluteNumber = null,
    bool IsSpecial = false);
