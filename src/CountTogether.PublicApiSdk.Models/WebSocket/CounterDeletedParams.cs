using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models.WebSocket;

/// <summary>
/// Params payload for the "counterDeleted" WebSocket event.
/// </summary>
public sealed class CounterDeletedParams
{
    /// <summary>
    /// The id of the deleted counter (UUID).
    /// </summary>
    [JsonPropertyName("counterId")]
    public string CounterId { get; set; } = null!;
}

