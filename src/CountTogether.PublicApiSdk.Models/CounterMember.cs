using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models;

/// <summary>
/// Represents a member of a counter.
/// </summary>
public sealed class CounterMember
{
    /// <summary>
    /// Identity of the member (UUID).
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = null!;

    /// <summary>
    /// User display name at time of retrieval.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Whether the user has admin privileges for the counter.
    /// </summary>
    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }
}

