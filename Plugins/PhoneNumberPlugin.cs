using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ZoomPhoneAgent.Services;

namespace ZoomPhoneAgent.Plugins;

/// <summary>
/// Plugin for querying Zoom Phone numbers — assigned, unassigned, and by site.
/// </summary>
public sealed class PhoneNumberPlugin
{
    private readonly ZoomPhoneApiService _api;

    public PhoneNumberPlugin(ZoomPhoneApiService api) => _api = api;

    [KernelFunction, Description("List phone numbers in the Zoom Phone system. Can filter by type (assigned/unassigned) to find available numbers.")]
    public async Task<string> ListPhoneNumbers(
        [Description("Filter: 'assigned' for numbers in use, 'unassigned' for available numbers, or leave empty for all.")] string? type = null,
        [Description("Page number, starts at 1.")] int pageNumber = 1,
        [Description("Results per page, max 100.")] int pageSize = 30)
    {
        var query = new Dictionary<string, string>
        {
            ["page_size"] = pageSize.ToString(),
            ["page_number"] = pageNumber.ToString()
        };
        if (!string.IsNullOrEmpty(type))
            query["type"] = type;

        var result = await _api.GetAsync("/phone/numbers", query);
        return FormatNumbers(result);
    }

    [KernelFunction, Description("Search for a specific phone number to see who it's assigned to and its details.")]
    public async Task<string> SearchPhoneNumber(
        [Description("The phone number to search for (e.g., '9569846014' or '+19569846014').")] string phoneNumber)
    {
        var query = new Dictionary<string, string>
        {
            ["keyword"] = phoneNumber,
            ["page_size"] = "10"
        };
        var result = await _api.GetAsync("/phone/numbers", query);
        return FormatNumbers(result);
    }

    private static string FormatNumbers(JsonElement result)
    {
        if (!result.TryGetProperty("phone_numbers", out var numbers))
            return "No phone numbers found.";

        var lines = new List<string>();
        foreach (var num in numbers.EnumerateArray())
        {
            var number = num.TryGetProperty("number", out var n) ? n.GetString() : "N/A";
            var displayName = num.TryGetProperty("display_name", out var dn) ? dn.GetString() : "Unassigned";
            var assignee = num.TryGetProperty("assignee", out var a) && a.TryGetProperty("name", out var an) ? an.GetString() : null;
            var site = num.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var sn) ? sn.GetString() : "N/A";
            var status = num.TryGetProperty("status", out var st) ? st.GetString() : "N/A";

            var owner = assignee ?? displayName ?? "Unassigned";
            lines.Add($"- {number} | Assigned To: {owner} | Site: {site} | Status: {status}");
        }

        var total = result.TryGetProperty("total_records", out var tr) ? tr.GetInt32() : lines.Count;
        lines.Insert(0, $"Found {total} phone number(s):");
        return string.Join("\n", lines);
    }
}
