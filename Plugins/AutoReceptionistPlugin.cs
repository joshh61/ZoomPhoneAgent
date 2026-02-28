using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ZoomPhoneAgent.Services;

namespace ZoomPhoneAgent.Plugins;

/// <summary>
/// Plugin for querying Zoom Phone auto receptionists (IVR systems).
/// </summary>
public sealed class AutoReceptionistPlugin
{
    private readonly ZoomPhoneApiService _api;

    public AutoReceptionistPlugin(ZoomPhoneApiService api) => _api = api;

    [KernelFunction, Description("List all auto receptionists in the Zoom Phone system. Shows name, extension, site, and status.")]
    public async Task<string> ListAutoReceptionists(
        [Description("Page number, starts at 1.")] int pageNumber = 1,
        [Description("Results per page, max 100.")] int pageSize = 50)
    {
        var query = new Dictionary<string, string>
        {
            ["page_size"] = pageSize.ToString(),
            ["page_number"] = pageNumber.ToString()
        };
        var result = await _api.GetAsync("/phone/auto_receptionists", query);

        if (!result.TryGetProperty("auto_receptionists", out var ars))
            return "No auto receptionists found.";

        var lines = new List<string>();
        foreach (var ar in ars.EnumerateArray())
        {
            var name = ar.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
            var ext = ar.TryGetProperty("extension_number", out var x) ? x.ToString() : "N/A";
            var site = ar.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var sn) ? sn.GetString() : "N/A";
            var status = ar.TryGetProperty("status", out var st) ? st.GetString() : "N/A";
            lines.Add($"- {name} | Ext: {ext} | Site: {site} | Status: {status}");
        }

        var total = result.TryGetProperty("total_records", out var tr) ? tr.GetInt32() : lines.Count;
        lines.Insert(0, $"Found {total} auto receptionist(s):");
        return string.Join("\n", lines);
    }

    [KernelFunction, Description("Get detailed configuration of a specific auto receptionist, including IVR menu options and call routing.")]
    public async Task<string> GetAutoReceptionistDetails(
        [Description("The auto receptionist ID. Use ListAutoReceptionists first to find the ID.")] string autoReceptionistId)
    {
        var result = await _api.GetAsync($"/phone/auto_receptionists/{Uri.EscapeDataString(autoReceptionistId)}");
        return result.ToString();
    }
}
