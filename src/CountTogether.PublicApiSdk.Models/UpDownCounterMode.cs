using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models;

/// <summary>
/// Defines the counting direction for an UpDown counter.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UpDownCounterMode
{
    /// <summary>
    /// Value monotonically increases.
    /// </summary>
    OnlyUp,

    /// <summary>
    /// Value monotonically decreases.
    /// </summary>
    OnlyDown,

    /// <summary>
    /// Value may move in both directions.
    /// </summary>
    UpAndDown
}

