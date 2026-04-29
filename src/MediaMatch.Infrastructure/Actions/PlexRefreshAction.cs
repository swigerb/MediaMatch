using System.Net.Http.Headers;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Actions;

/// <summary>
/// Post-process action that triggers a Plex library section refresh via HTTP.
/// </summary>
public sealed class PlexRefreshAction : IPostProcessAction
{
    private readonly HttpClient _http;
    private readonly PlexSettings _settings;
    private readonly ILogger<PlexRefreshAction> _logger;

    /// <inheritdoc />
    public string Name => "plex-refresh";

    /// <inheritdoc />
    public bool IsAvailable => !string.IsNullOrWhiteSpace(_settings.Url) && !string.IsNullOrWhiteSpace(_settings.Token);

    /// <summary>
    /// Initializes a new instance of the <see cref="PlexRefreshAction"/> class.
    /// </summary>
    /// <param name="http">The HTTP client for sending refresh requests.</param>
    /// <param name="settings">Plex connection settings.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public PlexRefreshAction(HttpClient http, PlexSettings settings, ILogger<PlexRefreshAction>? logger = null)
    {
        _http = http;
        _settings = settings;
        _logger = logger ?? NullLogger<PlexRefreshAction>.Instance;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(FileOrganizationResult result, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Plex refresh skipped — URL or token not configured");
            return;
        }

        var baseUrl = _settings.Url.TrimEnd('/');

        foreach (var sectionId in _settings.LibrarySectionIds)
        {
            var url = $"{baseUrl}/library/sections/{sectionId}/refresh";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Plex-Token", _settings.Token);

            try
            {
                using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
                _logger.LogInformation("Plex refresh section {Section}: {Status}", sectionId, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Plex refresh failed for section {Section}", sectionId);
            }
        }
    }
}
