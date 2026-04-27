using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

public interface IArtworkProvider
{
    string Name { get; }

    Task<IReadOnlyList<Artwork>> GetArtworkAsync(int tvdbId, ArtworkType? type = null, CancellationToken ct = default);

    Task<IReadOnlyList<Artwork>> GetMovieArtworkAsync(int tmdbId, ArtworkType? type = null, CancellationToken ct = default);
}
