using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models;

/// <summary>
/// Represents a counter returned by the REST API.
/// </summary>
public sealed class Counter
{
    /// <summary>
    /// Unique counter id (UUID v7, server generated).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Display name of the counter (5–40 chars).
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// The type of counter.
    /// </summary>
    [JsonPropertyName("type")]
    public CounterType Type { get; set; }

    /// <summary>
    /// Current membership list.
    /// </summary>
    [JsonPropertyName("members")]
    public List<CounterMember> Members { get; set; } = new();

    /// <summary>
    /// Type-specific data payload. Shape depends on <see cref="Type"/>.
    /// May be <c>null</c> for unrecognized or future counter types.
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}

