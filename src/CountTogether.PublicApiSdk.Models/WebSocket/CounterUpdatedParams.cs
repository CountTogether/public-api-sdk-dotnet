using System;
using System.Text.Json.Serialization;
using CountTogether.PublicApiSdk.Models.Converters;

namespace CountTogether.PublicApiSdk.Models.WebSocket;

/// <summary>
/// Params payload for the "counterUpdated" WebSocket event.
/// </summary>
public sealed class CounterUpdatedParams
{
    /// <summary>
    /// The id of the updated counter.
    /// </summary>
    [JsonPropertyName("counterId")]
    public Guid CounterId { get; set; }

    /// <summary>
    /// Display name of the counter.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The type of counter.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(CounterTypeJsonConverter))]
    public CounterType Type { get; set; }

    /// <summary>
    /// The current value, see <see cref="Counter.Value"/>.
    /// </summary>
    [JsonPropertyName("value")]
    public long? Value { get; set; }

    /// <summary>
    /// Start date of a <see cref="CounterType.FromDate"/> counter.
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// End date of a <see cref="CounterType.ToDate"/> counter.
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateOnly? EndDate { get; set; }
}
