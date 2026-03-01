using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models.WebSocket;

/// <summary>
/// Params payload for the "counterMemberlistChanged" WebSocket event.
/// </summary>
public sealed class CounterMemberlistChangedParams
{
    /// <summary>
    /// The id of the counter whose member list changed (UUID).
    /// </summary>
    [JsonPropertyName("counterId")]
    public string CounterId { get; set; } = null!;
}
















