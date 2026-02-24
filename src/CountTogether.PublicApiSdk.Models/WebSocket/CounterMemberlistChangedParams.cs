using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models.WebSocket;

/// <summary>
/// Defines the type of a counter.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CounterType
{
    /// <summary>
    /// Numeric counter with optional increment/decrement behavior.
    /// </summary>
    UpDown,

    /// <summary>
    /// Tracks duration since a fixed start date.
    /// </summary>
    FromDate,

    /// <summary>
    /// Tracks remaining time until a target date.
    /// </summary>
    ToDate
}
















