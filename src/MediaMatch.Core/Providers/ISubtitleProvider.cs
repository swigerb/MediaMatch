using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

public interface ISubtitleProvider
{
    string Name { get; }

    Task<IReadOnlyList<SubtitleDescriptor>> SearchAsync(string query, string language, CancellationToken ct = default);

    Task<IReadOnlyList<SubtitleDescriptor>> SearchByHashAsync(string movieHash, long fileSize, string language, CancellationToken ct = default);

    Task<Stream> DownloadAsync(SubtitleDescriptor subtitle, CancellationToken ct = default);
}
