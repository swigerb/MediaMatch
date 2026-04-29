using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MediaMatch.Infrastructure.Http;

/// <summary>
/// Resilient HTTP client wrapper with retry logic, rate limiting, and
/// consistent user-agent headers for metadata API calls.
/// </summary>
public sealed class MediaMatchHttpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<MediaMatchHttpClient> _logger;
    private readonly int _maxRetries;

    // Simple sliding-window rate limiter: TMDb allows 40 req / 10 sec.
    private readonly SemaphoreSlim _rateLimitGate = new(1, 1);
    private readonly Queue<DateTimeOffset> _requestTimestamps = new();
    private const int RateLimitWindow = 10; // seconds
    private const int RateLimitMax = 38;    // leave 2-request headroom

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaMatchHttpClient"/> class.
    /// </summary>
    /// <param name="http">The underlying <see cref="HttpClient"/> to use for requests.</param>
    /// <param name="logger">Logger for diagnostics and retry information.</param>
    /// <param name="maxRetries">Maximum number of retries for transient failures.</param>
    public MediaMatchHttpClient(HttpClient http, ILogger<MediaMatchHttpClient> logger, int maxRetries = 3)
    {
        _http = http;
        _logger = logger;
        _maxRetries = maxRetries;

        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MediaMatch/1.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    /// <summary>
    /// Sends a GET request and deserialises the JSON response.
    /// Handles transient failures and rate-limit back-off automatically.
    /// </summary>
    public Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
        => GetAsync<T>(url, headers: null, ct);

    /// <summary>
    /// Sends a GET request with optional custom headers and deserialises the JSON response.
    /// </summary>
    public async Task<T?> GetAsync<T>(string url, IDictionary<string, string>? headers, CancellationToken ct = default)
    {
        await EnforceRateLimitAsync(ct).ConfigureAwait(false);

        int attempt = 0;
        while (true)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                ApplyHeaders(request, headers);

                using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                    _logger.LogWarning("Rate limited on {Url}. Backing off {Seconds}s", url, retryAfter.TotalSeconds);
                    await Task.Delay(retryAfter, ct).ConfigureAwait(false);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries && IsTransient(ex))
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Transient failure on {Url}, retry {Attempt}/{Max} in {Delay}s",
                    url, attempt, _maxRetries, delay.TotalSeconds);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Sends a POST request with a JSON body and deserialises the response.
    /// </summary>
    public Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
        => PostAsync<TRequest, TResponse>(url, body, headers: null, ct);

    /// <summary>
    /// Sends a POST request with a JSON body and optional custom headers, then deserialises the response.
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string url,
        TRequest body,
        IDictionary<string, string>? headers,
        CancellationToken ct = default)
    {
        await EnforceRateLimitAsync(ct).ConfigureAwait(false);

        int attempt = 0;
        while (true)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(body, options: JsonOptions)
                };
                ApplyHeaders(request, headers);

                using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                    await Task.Delay(retryAfter, ct).ConfigureAwait(false);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries && IsTransient(ex))
            {
                attempt++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct).ConfigureAwait(false);
            }
        }
    }

    private static void ApplyHeaders(HttpRequestMessage request, IDictionary<string, string>? headers)
    {
        if (headers is null) return;
        foreach (var (key, value) in headers)
        {
            // TryAddWithoutValidation tolerates non-standard header values (e.g. raw bearer tokens).
            request.Headers.TryAddWithoutValidation(key, value);
        }
    }

    private async Task EnforceRateLimitAsync(CancellationToken ct)
    {
        await _rateLimitGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var cutoff = now.AddSeconds(-RateLimitWindow);

            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < cutoff)
                _requestTimestamps.Dequeue();

            if (_requestTimestamps.Count >= RateLimitMax)
            {
                var oldest = _requestTimestamps.Peek();
                var waitTime = oldest.AddSeconds(RateLimitWindow) - now;
                if (waitTime > TimeSpan.Zero)
                    await Task.Delay(waitTime, ct).ConfigureAwait(false);
            }

            _requestTimestamps.Enqueue(DateTimeOffset.UtcNow);
        }
        finally
        {
            _rateLimitGate.Release();
        }
    }

    private static bool IsTransient(HttpRequestException ex)
    {
        return ex.StatusCode is HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            or HttpStatusCode.RequestTimeout
            or null; // network-level failures
    }
}
