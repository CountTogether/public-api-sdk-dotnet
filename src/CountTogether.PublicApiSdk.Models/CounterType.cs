namespace CountTogether.PublicApiSdk.Models;

/// <summary>
/// Defines the type of a counter.
/// </summary>
public enum CounterType
{
    /// <summary>
    /// A counter type that this SDK version does not know.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Numeric counter that can be incremented and decremented.
    /// </summary>
    UpDown = 1,

    /// <summary>
    /// Counts the days since a start date.
    /// </summary>
    FromDate = 2,

    /// <summary>
    /// Counts the days until a target date.
    /// </summary>
    ToDate = 3
}
