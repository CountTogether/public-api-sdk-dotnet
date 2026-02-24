using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models;

/// <summary>
/// Data payload for a counter of type <see cref="CounterType.UpDown"/>.
/// </summary>
public sealed class PublicV1UpDownCounterData
{
    /// <summary>
    /// Current counter numeric value.
    /// </summary>
    [JsonPropertyName("value")]
    public long Value { get; set; }

    /// <summary>
    /// The counting direction mode.
    /// </summary>
    [JsonPropertyName("mode")]
    public UpDownCounterMode Mode { get; set; }
}

