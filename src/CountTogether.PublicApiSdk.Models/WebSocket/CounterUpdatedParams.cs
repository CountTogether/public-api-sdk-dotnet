using System.Text.Json;
using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models.WebSocket;

/// <summary>
/// Params payload for the "counterUpdated" WebSocket event.
/// </summary>
public sealed class CounterUpdatedParams
{
    /// <summary>
    /// The id of the updated counter (UUID).
    /// </summary>
    [JsonPropertyName("counterId")]
    public string CounterId { get; set; } = null!;

    /// <summary>
    /// Updated display name (optional, may be <c>null</c>).
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Updated type-specific data payload. May be <c>null</c>.
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}

