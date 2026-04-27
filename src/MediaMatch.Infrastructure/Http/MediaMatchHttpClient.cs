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
    /// Initialises a new <see cref="MediaMatchHttpClient"/>.
    /// </summary>
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
    public async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
    {
        await EnforceRateLimitAsync(ct);

        int attempt = 0;
        while (true)
        {
            try
            {
                var response = await _http.GetAsync(url, ct);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                    _logger.LogWarning("Rate limited on {Url}. Backing off {Seconds}s", url, retryAfter.TotalSeconds);
                    await Task.Delay(retryAfter, ct);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries && IsTransient(ex))
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Transient failure on {Url}, retry {Attempt}/{Max} in {Delay}s",
                    url, attempt, _maxRetries, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }
    }

    /// <summary>
    /// Sends a POST request with a JSON body and deserialises the response.
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
    {
        await EnforceRateLimitAsync(ct);

        int attempt = 0;
        while (true)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(url, body, JsonOptions, ct);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                    await Task.Delay(retryAfter, ct);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries && IsTransient(ex))
            {
                attempt++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }
    }

    private async Task EnforceRateLimitAsync(CancellationToken ct)
    {
        await _rateLimitGate.WaitAsync(ct);
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
                    await Task.Delay(waitTime, ct);
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
