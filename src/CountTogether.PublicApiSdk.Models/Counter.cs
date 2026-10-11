using System;
using System.Text.Json.Serialization;
using CountTogether.PublicApiSdk.Models.Converters;

namespace CountTogether.PublicApiSdk.Models;

/// <summary>
/// Represents a counter returned by the CountTogether Public API v2.
/// </summary>
public sealed class Counter
{
    /// <summary>
    /// Unique counter id (UUID v7, server generated).
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Display name of the counter.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of counter. <see cref="CounterType.Unknown"/> for counter types this SDK version does not know.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(CounterTypeJsonConverter))]
    public CounterType Type { get; set; }

    /// <summary>
    /// The current value. For <see cref="CounterType.UpDown"/> the counter value, for
    /// <see cref="CounterType.FromDate"/> the days elapsed since <see cref="StartDate"/> and for
    /// <see cref="CounterType.ToDate"/> the days remaining until <see cref="EndDate"/> (zero or negative once reached).
    /// Day values are calculated in the time zone configured on the client.
    /// </summary>
    [JsonPropertyName("value")]
    public long? Value { get; set; }

    /// <summary>
    /// Start date of a <see cref="CounterType.FromDate"/> counter, otherwise <c>null</c>.
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// End (target) date of a <see cref="CounterType.ToDate"/> counter, otherwise <c>null</c>.
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Creation time in UTC, or <c>null</c> when the API does not provide it.
    /// </summary>
    [JsonPropertyName("created")]
    [JsonConverter(typeof(NullableUtcDateTimeJsonConverter))]
    public DateTime? Created { get; set; }

    /// <summary>
    /// The calendar date of a date counter: <see cref="StartDate"/> for <see cref="CounterType.FromDate"/>,
    /// <see cref="EndDate"/> for <see cref="CounterType.ToDate"/>, otherwise <c>null</c>.
    /// </summary>
    [JsonIgnore]
    public DateOnly? Date => Type switch
    {
        CounterType.FromDate => StartDate,
        CounterType.ToDate => EndDate,
        _ => null
    };
}
