using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ZoomPhoneAgent.Services;

namespace ZoomPhoneAgent.Plugins;

/// <summary>
/// Plugin for querying Zoom Phone voicemails.
/// </summary>
public sealed class VoicemailPlugin
{
    private readonly ZoomPhoneApiService _api;

    public VoicemailPlugin(ZoomPhoneApiService api) => _api = api;

    [KernelFunction, Description("List voicemails for a specific user. Can filter by read/unread status. Use this to check who has unread voicemails.")]
    public async Task<string> GetUserVoicemails(
        [Description("The user's email address or Zoom user ID.")] string userId,
        [Description("Filter by status: 'read' or 'unread'. Leave empty for all.")] string? status = null,
        [Description("Page number, starts at 1.")] int pageNumber = 1,
        [Description("Results per page, max 100.")] int pageSize = 30)
    {
        var query = new Dictionary<string, string>
        {
            ["page_size"] = pageSize.ToString(),
            ["page_number"] = pageNumber.ToString()
        };
        if (!string.IsNullOrEmpty(status))
            query["status"] = status;

        var result = await _api.GetAsync($"/phone/users/{Uri.EscapeDataString(userId)}/voice_mails", query);

        if (!result.TryGetProperty("voice_mails", out var voicemails))
            return $"No voicemails found for user {userId}.";

        var lines = new List<string>();
        foreach (var vm in voicemails.EnumerateArray())
        {
            var callerName = vm.TryGetProperty("caller_name", out var cn) ? cn.GetString() : "Unknown";
            var callerNumber = vm.TryGetProperty("caller_number", out var cnum) ? cnum.GetString() : "N/A";
            var dateTime = vm.TryGetProperty("date_time", out var dt) ? dt.GetString() : "N/A";
            var duration = vm.TryGetProperty("duration", out var dur) ? dur.ToString() : "N/A";
            var vmStatus = vm.TryGetProperty("status", out var st) ? st.GetString() : "N/A";

            lines.Add($"- From: {callerName} ({callerNumber}) | {dateTime} | Duration: {duration}s | Status: {vmStatus}");
        }

        var total = result.TryGetProperty("total_records", out var tr) ? tr.GetInt32() : lines.Count;
        lines.Insert(0, $"Found {total} voicemail(s):");
        return string.Join("\n", lines);
    }
}
