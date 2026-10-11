using System;
using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models.WebSocket;

/// <summary>
/// Params payload for the "counterDeleted" WebSocket event.
/// </summary>
public sealed class CounterDeletedParams
{
    /// <summary>
    /// The id of the deleted counter.
    /// </summary>
    [JsonPropertyName("counterId")]
    public Guid CounterId { get; set; }
}
