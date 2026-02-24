using System.Text.Json;
using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models.WebSocket;

/// <summary>
/// Generic JSON-RPC 2.0 notification (no id) as received over WebSocket.
/// </summary>
public sealed class JsonRpcNotification
{
    /// <summary>
    /// Always "2.0".
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// Event identifier (e.g. "counterUpdated", "counterDeleted", "counterMemberlistChanged").
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = null!;

    /// <summary>
    /// Event-specific payload.
    /// </summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
}

