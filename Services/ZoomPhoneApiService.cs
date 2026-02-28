using System.Net.Http.Headers;
using System.Text.Json;

namespace ZoomPhoneAgent.Services;

/// <summary>
/// HTTP client wrapper for the Zoom Phone REST API.
/// All methods are read-only (GET requests only) for the MVP.
/// </summary>
public sealed class ZoomPhoneApiService
{
    private const string BaseUrl = "https://api.zoom.us/v2";

    private readonly ZoomAuthService _auth;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZoomPhoneApiService> _logger;

    public ZoomPhoneApiService(ZoomAuthService auth, HttpClient httpClient, ILogger<ZoomPhoneApiService> logger)
    {
        _auth = auth;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Makes an authenticated GET request to the Zoom Phone API.
    /// </summary>
    public async Task<JsonElement> GetAsync(string endpoint, Dictionary<string, string>? queryParams = null)
    {
        var token = await _auth.GetAccessTokenAsync();

        var url = $"{BaseUrl}{endpoint}";
        if (queryParams is { Count: > 0 })
        {
            var queryString = string.Join("&",
                queryParams.Where(kv => !string.IsNullOrEmpty(kv.Value))
                           .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            url = $"{url}?{queryString}";
        }

        _logger.LogInformation("Zoom API GET: {Url}", url);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Zoom API error {StatusCode}: {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException($"Zoom API returned {response.StatusCode}: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
