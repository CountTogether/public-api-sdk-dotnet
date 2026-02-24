using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models;

/// <summary>
/// Structured JSON error response returned by the API.
/// </summary>
public sealed class ErrorObject
{
    /// <summary>
    /// Human-readable error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Machine-readable error code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }
}

