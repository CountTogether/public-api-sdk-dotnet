namespace CountTogether.PublicApiSdk.Abstractions.Client;

public class CountTogetherClientConfiguration
{
    public string ApiToken { get; set; } = null!;
    
    public string? ApiUrlOverride { get; set; }
    
    /// <summary>
    /// When <c>true</c>, the WebSocket connection will automatically reconnect after an unexpected disconnection. Defaults to <c>true</c>.
    /// </summary>
    public bool AutoReconnect { get; set; } = true;
}