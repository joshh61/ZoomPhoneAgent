using System.Net.Http.Headers;
using System.Text.Json;

namespace ZoomPhoneAgent.Services;

/// <summary>
/// Handles Zoom Server-to-Server OAuth token acquisition and caching.
/// Tokens are valid for 1 hour; this service auto-refreshes before expiry.
/// </summary>
public sealed class ZoomAuthService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZoomAuthService> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ZoomAuthService(IConfiguration config, HttpClient httpClient, ILogger<ZoomAuthService> logger)
    {
        _config = config;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        // Return cached token if still valid (with 5 min buffer)
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
            return _cachedToken;

        await _lock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
                return _cachedToken;

            var accountId = _config["Zoom:AccountId"]!;
            var clientId = _config["Zoom:ClientId"]!;
            var clientSecret = _config["Zoom:ClientSecret"]!;

            var credentials = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://zoom.us/oauth/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "account_credentials",
                ["account_id"] = accountId
            });

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            _cachedToken = json.GetProperty("access_token").GetString()!;
            var expiresIn = json.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

            _logger.LogInformation("Zoom OAuth token acquired, expires in {ExpiresIn}s", expiresIn);
            return _cachedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire Zoom OAuth token");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }
}
