using System.Net.Http.Headers;
using System.Text.Json;
using OpenRouter.NET.Models;
using OpenRouter.NET.Observability;

namespace OpenRouter.NET.Internal;

/// <summary>
/// Handles HTTP request configuration, header management, and error response handling
/// </summary>
internal class HttpRequestHandler
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string? _siteUrl;
    private readonly string? _siteName;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly OpenRouterTelemetryOptions _telemetryOptions;

    public HttpRequestHandler(
        HttpClient httpClient,
        string apiKey,
        string baseUrl,
        string? siteUrl,
        string? siteName,
        JsonSerializerOptions jsonOptions,
        OpenRouterTelemetryOptions telemetryOptions)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _siteUrl = siteUrl;
        _siteName = siteName;
        _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
        _telemetryOptions = telemetryOptions ?? throw new ArgumentNullException(nameof(telemetryOptions));
    }

    /// <summary>
    /// Adds authentication and custom headers to an HTTP request
    /// </summary>
    public void AddHeaders(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(_siteUrl))
        {
            request.Headers.Add("HTTP-Referer", _siteUrl);
        }

        if (!string.IsNullOrEmpty(_siteName))
        {
            request.Headers.Add("X-Title", _siteName);
        }
    }

    /// <summary>
    /// Handles error responses from the OpenRouter API
    /// </summary>
    public async Task HandleErrorResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();
        ErrorResponse? errorResponse = null;

        try
        {
            errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
        }
        catch
        {
            // Ignore deserialization errors
        }

        var errorMessage = errorResponse?.Error?.Message ?? response.ReasonPhrase ?? "Unknown error";

        switch ((int)response.StatusCode)
        {
            case 401:
                throw new OpenRouterAuthException($"Authentication failed: {errorMessage}");
            case 429:
                int? retryAfter = null;
                if (response.Headers.TryGetValues("Retry-After", out var values))
                {
                    if (int.TryParse(values.First(), out var seconds))
                    {
                        retryAfter = seconds;
                    }
                }
                throw new OpenRouterRateLimitException($"Rate limit exceeded: {errorMessage}", retryAfter);
            case 400:
                throw new OpenRouterBadRequestException($"Bad request: {errorMessage}");
            case 404:
                throw new OpenRouterModelNotFoundException("unknown", $"Not found: {errorMessage}");
            case >= 500:
                throw new OpenRouterServerException($"Server error: {errorMessage}");
            default:
                throw new OpenRouterException($"Request failed with status {response.StatusCode}: {errorMessage}", (int)response.StatusCode);
        }
    }

    public string BaseUrl => _baseUrl;
    public HttpClient HttpClient => _httpClient;
    public JsonSerializerOptions JsonOptions => _jsonOptions;
}
