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
}