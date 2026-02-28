using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ZoomPhoneAgent.Services;

namespace ZoomPhoneAgent.Plugins;

/// <summary>
/// Plugin for querying Zoom Phone call queues and their members.
/// </summary>
public sealed class CallQueuePlugin
{
    private readonly ZoomPhoneApiService _api;

    public CallQueuePlugin(ZoomPhoneApiService api) => _api = api;

    [KernelFunction, Description("List all call queues in the Zoom Phone system. Shows queue name, extension, site, and status.")]
    public async Task<string> ListCallQueues(
        [Description("Page number, starts at 1.")] int pageNumber = 1,
        [Description("Results per page, max 100.")] int pageSize = 50)
    {
        var query = new Dictionary<string, string>
        {
            ["page_size"] = pageSize.ToString(),
            ["page_number"] = pageNumber.ToString()
        };
        var result = await _api.GetAsync("/phone/call_queues", query);

        if (!result.TryGetProperty("call_queues", out var queues))
            return "No call queues found.";

        var lines = new List<string>();
        foreach (var q in queues.EnumerateArray())
        {
            var name = q.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
            var ext = q.TryGetProperty("extension_number", out var x) ? x.ToString() : "N/A";
            var site = q.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var sn) ? sn.GetString() : "N/A";
            var status = q.TryGetProperty("status", out var st) ? st.GetString() : "N/A";
            lines.Add($"- {name} | Ext: {ext} | Site: {site} | Status: {status}");
        }

        var total = result.TryGetProperty("total_records", out var tr) ? tr.GetInt32() : lines.Count;
        lines.Insert(0, $"Found {total} call queue(s):");
        return string.Join("\n", lines);
    }

    [KernelFunction, Description("Get details and members of a specific call queue. Use this to see who is in a queue.")]
    public async Task<string> GetCallQueueDetails(
        [Description("The call queue ID. Use ListCallQueues first to find the ID.")] string callQueueId)
    {
        var result = await _api.GetAsync($"/phone/call_queues/{Uri.EscapeDataString(callQueueId)}");
        return FormatQueueDetails(result);
    }

    private static string FormatQueueDetails(JsonElement queue)
    {
        var lines = new List<string>();

        var name = queue.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
        lines.Add($"Queue: {name}");

        if (queue.TryGetProperty("extension_number", out var ext)) lines.Add($"Extension: {ext}");
        if (queue.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var sn)) lines.Add($"Site: {sn.GetString()}");
        if (queue.TryGetProperty("status", out var st)) lines.Add($"Status: {st.GetString()}");
        if (queue.TryGetProperty("phone_numbers", out var nums) && nums.GetArrayLength() > 0)
        {
            var phoneNums = new List<string>();
            foreach (var num in nums.EnumerateArray())
                if (num.TryGetProperty("number", out var pn)) phoneNums.Add(pn.GetString() ?? "");
            lines.Add($"Phone Numbers: {string.Join(", ", phoneNums)}");
        }
        if (queue.TryGetProperty("members", out var members))
        {
            lines.Add($"Members ({members.GetArrayLength()}):");
            foreach (var m in members.EnumerateArray())
            {
                var mName = m.TryGetProperty("name", out var mn) ? mn.GetString() : "Unknown";
                var mExt = m.TryGetProperty("extension_number", out var mx) ? mx.ToString() : "N/A";
                var receive = m.TryGetProperty("receive_call", out var rc) ? rc.GetBoolean() : false;
                lines.Add($"  - {mName} | Ext: {mExt} | Receiving: {(receive ? "Yes" : "No")}");
            }
        }

        return string.Join("\n", lines);
    }

    [KernelFunction, Description("List the members (agents) assigned to a specific call queue.")]
    public async Task<string> ListCallQueueMembers(
        [Description("The call queue ID.")] string callQueueId)
    {
        var result = await _api.GetAsync($"/phone/call_queues/{Uri.EscapeDataString(callQueueId)}/members");

        if (!result.TryGetProperty("call_queue_members", out var members))
            return "No members found in this call queue.";

        var lines = new List<string>();
        foreach (var m in members.EnumerateArray())
        {
            var name = m.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
            var ext = m.TryGetProperty("extension_number", out var x) ? x.ToString() : "N/A";
            var receive = m.TryGetProperty("receive_call", out var rc) ? rc.GetBoolean() : false;
            lines.Add($"- {name} | Ext: {ext} | Receiving Calls: {(receive ? "Yes" : "No")}");
        }

        lines.Insert(0, $"Queue has {lines.Count} member(s):");
        return string.Join("\n", lines);
    }
}
