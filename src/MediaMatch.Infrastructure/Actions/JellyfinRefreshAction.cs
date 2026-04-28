using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Actions;

/// <summary>
/// Post-process action that triggers a Jellyfin library refresh via HTTP.
/// </summary>
public sealed class JellyfinRefreshAction : IPostProcessAction
{
    private readonly HttpClient _http;
    private readonly JellyfinSettings _settings;
    private readonly ILogger<JellyfinRefreshAction> _logger;

    /// <inheritdoc />
    public string Name => "jellyfin-refresh";

    /// <inheritdoc />
    public bool IsAvailable => !string.IsNullOrWhiteSpace(_settings.Url) && !string.IsNullOrWhiteSpace(_settings.ApiKey);

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinRefreshAction"/> class.
    /// </summary>
    /// <param name="http">The HTTP client for sending refresh requests.</param>
    /// <param name="settings">Jellyfin connection settings.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public JellyfinRefreshAction(HttpClient http, JellyfinSettings settings, ILogger<JellyfinRefreshAction>? logger = null)
    {
        _http = http;
        _settings = settings;
        _logger = logger ?? NullLogger<JellyfinRefreshAction>.Instance;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(FileOrganizationResult result, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Jellyfin refresh skipped — URL or API key not configured");
            return;
        }

        var baseUrl = _settings.Url.TrimEnd('/');
        var url = $"{baseUrl}/Library/Refresh";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-Emby-Authorization", $"MediaBrowser Token=\"{_settings.ApiKey}\"");

        try
        {
            var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            _logger.LogInformation("Jellyfin library refresh: {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Jellyfin library refresh failed");
        }
    }
}
