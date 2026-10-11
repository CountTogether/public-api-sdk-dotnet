using System;
using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models;

/// <summary>
/// Partial update of a counter. Only fields that are set are sent; at least one field is required and each
/// field must be applicable to the counter type.
/// </summary>
public sealed class UpdateCounterRequest
{
    /// <summary>
    /// New display name (3–40 characters).
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// New value of a <see cref="CounterType.UpDown"/> counter.
    /// Note: the API currently resets the counter's step size to 1 when the value is set.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Value { get; set; }

    /// <summary>
    /// New start date of a <see cref="CounterType.FromDate"/> counter (must not be in the future).
    /// </summary>
    [JsonPropertyName("startDate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// New end date of a <see cref="CounterType.ToDate"/> counter (must not be in the past).
    /// </summary>
    [JsonPropertyName("endDate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? EndDate { get; set; }
}
