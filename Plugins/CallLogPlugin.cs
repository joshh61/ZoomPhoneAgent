using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ZoomPhoneAgent.Services;

namespace ZoomPhoneAgent.Plugins;

/// <summary>
/// Plugin for querying Zoom Phone call logs and call history.
/// </summary>
public sealed class CallLogPlugin
{
    private readonly ZoomPhoneApiService _api;

    public CallLogPlugin(ZoomPhoneApiService api) => _api = api;

    [KernelFunction, Description("Get call logs for the account. Can filter by date range, call direction, and call result. Use this for reporting on missed calls, call volume, etc.")]
    public async Task<string> GetCallLogs(
        [Description("Start date in YYYY-MM-DD format (e.g., '2026-02-20').")] string from,
        [Description("End date in YYYY-MM-DD format (e.g., '2026-02-27').")] string to,
        [Description("Filter by call direction: 'inbound', 'outbound', or leave empty for all.")] string? direction = null,
        [Description("Filter by call result: 'answered', 'missed', 'voicemail', 'cancelled', 'blocked', 'call_failed', or leave empty for all.")] string? callResult = null,
        [Description("Page number, starts at 1.")] int pageNumber = 1,
        [Description("Results per page, max 100.")] int pageSize = 30)
    {
        var query = new Dictionary<string, string>
        {
            ["from"] = from,
            ["to"] = to,
            ["page_size"] = pageSize.ToString(),
            ["page_number"] = pageNumber.ToString()
        };
        if (!string.IsNullOrEmpty(direction))
            query["direction"] = direction;
        if (!string.IsNullOrEmpty(callResult))
            query["call_result"] = callResult;

        var result = await _api.GetAsync("/phone/call_history", query);
        return FormatCallLogs(result);
    }

    [KernelFunction, Description("Get call logs for a specific user by their email or user ID. Useful for checking a specific person's call activity.")]
    public async Task<string> GetUserCallLogs(
        [Description("The user's email address or Zoom user ID.")] string userId,
        [Description("Start date in YYYY-MM-DD format.")] string from,
        [Description("End date in YYYY-MM-DD format.")] string to,
        [Description("Page number, starts at 1.")] int pageNumber = 1,
        [Description("Results per page, max 100.")] int pageSize = 30)
    {
        var query = new Dictionary<string, string>
        {
            ["from"] = from,
            ["to"] = to,
            ["page_size"] = pageSize.ToString(),
            ["page_number"] = pageNumber.ToString()
        };
        var result = await _api.GetAsync($"/phone/users/{Uri.EscapeDataString(userId)}/call_logs", query);
        return FormatCallLogs(result);
    }

    private static string FormatCallLogs(JsonElement result)
    {
        if (!result.TryGetProperty("call_logs", out var logs))
            return "No call logs found for the specified criteria.";

        var lines = new List<string>();
        foreach (var log in logs.EnumerateArray())
        {
            var direction = log.TryGetProperty("direction", out var d) ? d.GetString() : "N/A";
            var callerName = log.TryGetProperty("caller_name", out var cn) ? cn.GetString() : "Unknown";
            var calleeName = log.TryGetProperty("callee_name", out var cen) ? cen.GetString() : "Unknown";
            var startTime = log.TryGetProperty("date_time", out var dt) ? dt.GetString() : "N/A";
            var duration = log.TryGetProperty("duration", out var dur) ? dur.ToString() : "N/A";
            var callResult = log.TryGetProperty("result", out var r) ? r.GetString() : "N/A";
            var site = log.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var sn) ? sn.GetString() : "N/A";

            lines.Add($"- [{direction}] {callerName} → {calleeName} | {startTime} | Duration: {duration}s | Result: {callResult} | Site: {site}");
        }

        var total = result.TryGetProperty("total_records", out var tr) ? tr.GetInt32() : lines.Count;
        lines.Insert(0, $"Found {total} call log(s):");
        return string.Join("\n", lines);
    }
}
