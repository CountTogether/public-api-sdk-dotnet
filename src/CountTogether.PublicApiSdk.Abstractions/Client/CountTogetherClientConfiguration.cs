using System;

namespace CountTogether.PublicApiSdk.Abstractions.Client;

/// <summary>
/// The configuration class for the Count Together client, containing settings that control the client's behavior and connection to the Count Together Public API. This class is used when starting the client to provide necessary information such as the API token and connection options.
/// </summary>
public class CountTogetherClientConfiguration
{
    /// <summary>
    /// The API token used for authenticating with the Count Together Public API. This token is required and must be provided before starting the client.
    /// </summary>
    public string ApiToken { get; set; } = null!;
    
    /// <summary>
    /// When <c>true</c>, the WebSocket connection will automatically reconnect after an unexpected disconnection. Defaults to <c>true</c>.
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// The time zone (IANA id such as <c>Europe/Berlin</c>, or a Windows id) used by the API to calculate the day
    /// values of date counters. Defaults to the local time zone of this machine (see <see cref="DetectLocalTimeZone()"/>).
    /// Set to <c>null</c> to use the time zone stored in the CountTogether account. An unknown id makes the API fall
    /// back to UTC. The WebSocket connection keeps the UTC offset from the time it connected.
    /// </summary>
    public string? TimeZone { get; set; } = DetectLocalTimeZone();

    /// <summary>
    /// Returns the id of the local time zone as an IANA id where possible, or <c>null</c> if it cannot be determined.
    /// </summary>
    public static string? DetectLocalTimeZone() => DetectLocalTimeZone(TimeZoneInfo.Local);

    internal static string? DetectLocalTimeZone(TimeZoneInfo timeZone)
    {
        var id = timeZone.Id;
        if (string.IsNullOrWhiteSpace(id) || string.Equals(id, "Local", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (timeZone.HasIanaId)
        {
            return id;
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId))
        {
            return ianaId;
        }

        return IsSystemTimeZone(id) ? id : null;
    }

    private static bool IsSystemTimeZone(string id)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
