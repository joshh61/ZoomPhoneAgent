using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ZoomPhoneAgent.Services;

namespace ZoomPhoneAgent.Plugins;

/// <summary>
/// Plugin for querying Zoom Phone devices (desk phones, conference phones).
/// </summary>
public sealed class DevicePlugin
{
    private readonly ZoomPhoneApiService _api;

    public DevicePlugin(ZoomPhoneApiService api) => _api = api;

    [KernelFunction, Description("List Zoom Phone devices (desk phones). Can filter by status (online/offline) or type. Use this to find unassigned phones or check device status.")]
    public async Task<string> ListDevices(
        [Description("Filter by device status: 'online' or 'offline'. Leave empty for all.")] string? status = null,
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

        var result = await _api.GetAsync("/phone/devices", query);

        if (!result.TryGetProperty("devices", out var devices))
            return "No devices found.";

        var lines = new List<string>();
        foreach (var dev in devices.EnumerateArray())
        {
            var displayName = dev.TryGetProperty("display_name", out var dn) ? dn.GetString() : "Unknown";
            var brand = dev.TryGetProperty("brand", out var b) ? b.GetString() : "N/A";
            var model = dev.TryGetProperty("model", out var m) ? m.GetString() : "N/A";
            var macAddress = dev.TryGetProperty("mac_address", out var mac) ? mac.GetString() : "N/A";
            var assignee = dev.TryGetProperty("assignee", out var a) && a.TryGetProperty("name", out var an) ? an.GetString() : "Unassigned";
            var devStatus = dev.TryGetProperty("status", out var st) ? st.GetString() : "N/A";
            var site = dev.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var sn) ? sn.GetString() : "N/A";

            lines.Add($"- {displayName} | {brand} {model} | MAC: {macAddress} | Assigned: {assignee} | Status: {devStatus} | Site: {site}");
        }

        var total = result.TryGetProperty("total_records", out var tr) ? tr.GetInt32() : lines.Count;
        lines.Insert(0, $"Found {total} device(s):");
        return string.Join("\n", lines);
    }

    [KernelFunction, Description("Get detailed information about a specific device by its device ID.")]
    public async Task<string> GetDeviceDetails(
        [Description("The device ID. Use ListDevices first to find the ID.")] string deviceId)
    {
        var result = await _api.GetAsync($"/phone/devices/{Uri.EscapeDataString(deviceId)}");
        return FormatDeviceDetails(result);
    }

    private static string FormatDeviceDetails(JsonElement dev)
    {
        var lines = new List<string>();

        var displayName = dev.TryGetProperty("display_name", out var dn) ? dn.GetString() : "Unknown";
        lines.Add($"Device: {displayName}");

        if (dev.TryGetProperty("brand", out var b)) lines.Add($"Brand: {b.GetString()}");
        if (dev.TryGetProperty("model", out var m)) lines.Add($"Model: {m.GetString()}");
        if (dev.TryGetProperty("mac_address", out var mac)) lines.Add($"MAC Address: {mac.GetString()}");
        if (dev.TryGetProperty("serial_number", out var sn)) lines.Add($"Serial: {sn.GetString()}");
        if (dev.TryGetProperty("status", out var st)) lines.Add($"Status: {st.GetString()}");
        if (dev.TryGetProperty("firmware_version", out var fw)) lines.Add($"Firmware: {fw.GetString()}");
        if (dev.TryGetProperty("ip_address", out var ip)) lines.Add($"IP Address: {ip.GetString()}");
        if (dev.TryGetProperty("assignee", out var a) && a.TryGetProperty("name", out var an))
            lines.Add($"Assigned To: {an.GetString()}");
        if (dev.TryGetProperty("site", out var s) && s.TryGetProperty("name", out var siteName))
            lines.Add($"Site: {siteName.GetString()}");
        if (dev.TryGetProperty("provision_template", out var pt) && pt.TryGetProperty("name", out var ptName))
            lines.Add($"Provision Template: {ptName.GetString()}");

        return string.Join("\n", lines);
    }
}
