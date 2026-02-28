using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ZoomPhoneAgent.Services;

namespace ZoomPhoneAgent.Plugins;

/// <summary>
/// Plugin for querying Zoom Phone users and their settings.
/// </summary>
public sealed class UserPlugin
{
    private readonly ZoomPhoneApiService _api;

    public UserPlugin(ZoomPhoneApiService api) => _api = api;

    [KernelFunction, Description("List Zoom Phone users. Can filter by site, status, or department. Returns user name, email, extension, phone number, status, and site.")]
    public async Task<string> ListUsers(
        [Description("Filter by site name (e.g., 'Edinburg', 'Laredo', 'McAllen - 4311'). Leave empty for all sites.")] string? siteName = null,
        [Description("Page number for pagination, starts at 1.")] int pageNumber = 1,
        [Description("Number of results per page, max 100.")] int pageSize = 30)
    {
        var query = new Dictionary<string, string>
        {
            ["page_size"] = pageSize.ToString(),
            ["page_number"] = pageNumber.ToString()
        };
        if (!string.IsNullOrEmpty(siteName))
            query["site_id"] = siteName;

        var result = await _api.GetAsync("/phone/users", query);
        return FormatUsers(result);
    }

    [KernelFunction, Description("Search for a specific Zoom Phone user by name, email, or extension number. Use this when looking up a specific person.")]
    public async Task<string> SearchUser(
        [Description("The search keyword - can be a name (e.g., 'Dante Fox'), email (e.g., 'dfox@esc1.net'), or extension number (e.g., '6245').")] string keyword)
    {
        var query = new Dictionary<string, string>
        {
            ["keyword"] = keyword,
            ["page_size"] = "10"
        };
        var result = await _api.GetAsync("/phone/users", query);
        return FormatUsers(result);
    }

    [KernelFunction, Description("Get detailed information about a specific Zoom Phone user by their user ID or email address.")]
    public async Task<string> GetUserDetails(
        [Description("The user's email address or Zoom user ID.")] string userId)
    {
        var result = await _api.GetAsync($"/phone/users/{Uri.EscapeDataString(userId)}");
        return result.ToString();
    }

    private static string FormatUsers(JsonElement result)
    {
        if (!result.TryGetProperty("users", out var users))
            return "No users found.";

        var lines = new List<string>();
        foreach (var user in users.EnumerateArray())
        {
            var name = user.GetProperty("name").GetString() ?? "Unknown";
            var email = user.TryGetProperty("email", out var e) ? e.GetString() : "N/A";
            var ext = user.TryGetProperty("extension_number", out var x) ? x.ToString() : "N/A";
            var site = user.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var sn) ? sn.GetString() : "N/A";
            var status = user.TryGetProperty("status", out var st) ? st.GetString() : "N/A";
            lines.Add($"- {name} | Email: {email} | Ext: {ext} | Site: {site} | Status: {status}");
        }

        var totalRecords = result.TryGetProperty("total_records", out var tr) ? tr.GetInt32() : lines.Count;
        lines.Insert(0, $"Found {totalRecords} user(s):");
        return string.Join("\n", lines);
    }
}
