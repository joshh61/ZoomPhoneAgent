using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ZoomPhoneAgent.Services;

namespace ZoomPhoneAgent.Plugins;

/// <summary>
/// Plugin for querying Zoom Phone extensions across all types
/// (users, call queues, auto receptionists, common areas, shared line groups).
/// </summary>
public sealed class ExtensionPlugin
{
    private readonly ZoomPhoneApiService _api;

    public ExtensionPlugin(ZoomPhoneApiService api) => _api = api;

    [KernelFunction, Description("Search for an extension number to find out who or what it belongs to. Works for user extensions, call queues, auto receptionists, and more.")]
    public async Task<string> SearchExtension(
        [Description("The extension number to look up (e.g., '3056', '9000', '6245').")] string extensionNumber)
    {
        // Search across users first
        var userQuery = new Dictionary<string, string>
        {
            ["keyword"] = extensionNumber,
            ["page_size"] = "5"
        };
        var userResult = await _api.GetAsync("/phone/users", userQuery);

        if (userResult.TryGetProperty("users", out var users) && users.GetArrayLength() > 0)
        {
            var lines = new List<string> { $"Extension {extensionNumber} found:" };
            foreach (var user in users.EnumerateArray())
            {
                var name = user.GetProperty("name").GetString();
                var email = user.TryGetProperty("email", out var e) ? e.GetString() : "N/A";
                var ext = user.TryGetProperty("extension_number", out var x) ? x.ToString() : "N/A";
                var site = user.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var sn) ? sn.GetString() : "N/A";
                lines.Add($"- Type: User | Name: {name} | Email: {email} | Ext: {ext} | Site: {site}");
            }
            return string.Join("\n", lines);
        }

        // If not a user, check call queues
        var queueResult = await _api.GetAsync("/phone/call_queues", new Dictionary<string, string> { ["page_size"] = "100" });
        if (queueResult.TryGetProperty("call_queues", out var queues))
        {
            foreach (var q in queues.EnumerateArray())
            {
                var ext = q.TryGetProperty("extension_number", out var x) ? x.ToString() : "";
                if (ext == extensionNumber)
                {
                    var name = q.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                    var site = q.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var sn) ? sn.GetString() : "N/A";
                    return $"Extension {extensionNumber} found:\n- Type: Call Queue | Name: {name} | Ext: {ext} | Site: {site}";
                }
            }
        }

        // Check auto receptionists
        var arResult = await _api.GetAsync("/phone/auto_receptionists", new Dictionary<string, string> { ["page_size"] = "100" });
        if (arResult.TryGetProperty("auto_receptionists", out var ars))
        {
            foreach (var ar in ars.EnumerateArray())
            {
                var ext = ar.TryGetProperty("extension_number", out var x) ? x.ToString() : "";
                if (ext == extensionNumber)
                {
                    var name = ar.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                    var site = ar.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var sn) ? sn.GetString() : "N/A";
                    return $"Extension {extensionNumber} found:\n- Type: Auto Receptionist | Name: {name} | Ext: {ext} | Site: {site}";
                }
            }
        }

        return $"Extension {extensionNumber} was not found in users, call queues, or auto receptionists.";
    }
}
