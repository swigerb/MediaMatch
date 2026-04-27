using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

public interface IMovieProvider
{
    string Name { get; }

    Task<IReadOnlyList<Movie>> SearchAsync(string query, int? year = null, CancellationToken ct = default);

    Task<MovieInfo> GetMovieInfoAsync(Movie movie, CancellationToken ct = default);
}
