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

    [KernelFunction, Description("List Zoom Phone users. Can filter by site name. Returns user name, email, extension, phone number, status, and site.")]
    public async Task<string> ListUsers(
        [Description("Filter by site name (e.g., 'Edinburg', 'Laredo', 'McAllen-4311'). Leave empty for all sites.")] string? siteName = null,
        [Description("Page number for pagination, starts at 1.")] int pageNumber = 1,
        [Description("Number of results per page, max 100.")] int pageSize = 30)
    {
        var query = new Dictionary<string, string>
        {
            ["page_size"] = pageSize.ToString(),
            ["page_number"] = pageNumber.ToString()
        };

        // Zoom API requires a site UUID, not a name — look it up first
        if (!string.IsNullOrEmpty(siteName))
        {
            var siteId = await ResolveSiteIdAsync(siteName);
            if (siteId is not null)
                query["site_id"] = siteId;
        }

        var result = await _api.GetAsync("/phone/users", query);
        return FormatUsers(result);
    }

    /// <summary>
    /// Resolves a site name (e.g., "Edinburg") to its Zoom site UUID.
    /// Uses a case-insensitive contains match so partial names work.
    /// </summary>
    private async Task<string?> ResolveSiteIdAsync(string siteName)
    {
        var result = await _api.GetAsync("/phone/sites", new Dictionary<string, string> { ["page_size"] = "50" });
        if (!result.TryGetProperty("sites", out var sites))
            return null;

        foreach (var site in sites.EnumerateArray())
        {
            var name = site.TryGetProperty("name", out var n) ? n.GetString() : "";
            if (name is not null && name.Contains(siteName, StringComparison.OrdinalIgnoreCase))
            {
                return site.TryGetProperty("id", out var id) ? id.GetString() : null;
            }
        }
        return null;
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
        return FormatUserDetails(result);
    }

    private static string FormatUserDetails(JsonElement user)
    {
        var lines = new List<string>();

        var name = user.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
        lines.Add($"Name: {name}");

        if (user.TryGetProperty("email", out var e)) lines.Add($"Email: {e.GetString()}");
        if (user.TryGetProperty("extension_number", out var ext)) lines.Add($"Extension: {ext}");
        if (user.TryGetProperty("phone_numbers", out var nums) && nums.GetArrayLength() > 0)
        {
            var phoneNums = new List<string>();
            foreach (var num in nums.EnumerateArray())
                if (num.TryGetProperty("number", out var pn)) phoneNums.Add(pn.GetString() ?? "");
            lines.Add($"Phone Numbers: {string.Join(", ", phoneNums)}");
        }
        if (user.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var sn)) lines.Add($"Site: {sn.GetString()}");
        if (user.TryGetProperty("status", out var st)) lines.Add($"Status: {st.GetString()}");
        if (user.TryGetProperty("department", out var dept)) lines.Add($"Department: {dept.GetString()}");
        if (user.TryGetProperty("calling_plans", out var plans) && plans.GetArrayLength() > 0)
        {
            var planNames = new List<string>();
            foreach (var p in plans.EnumerateArray())
                if (p.TryGetProperty("name", out var pName)) planNames.Add(pName.GetString() ?? "");
            if (planNames.Count > 0) lines.Add($"Calling Plans: {string.Join(", ", planNames)}");
        }
        if (user.TryGetProperty("policy", out var policy) && policy.TryGetProperty("voicemail_access_member", out var vm))
            lines.Add($"Voicemail Enabled: {vm}");

        return string.Join("\n", lines);
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
