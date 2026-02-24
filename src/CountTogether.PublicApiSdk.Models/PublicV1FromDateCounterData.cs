using System;
using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models;

/// <summary>
/// Data payload for a counter of type <see cref="CounterType.FromDate"/>.
/// </summary>
public sealed class PublicV1FromDateCounterData
{
    /// <summary>
    /// Start date in UTC (ISO 8601).
    /// </summary>
    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; set; }
}

