namespace CountTogether.PublicApiSdk.Abstractions.Client;

public class CountTogetherClientConfiguration
{
    public string ApiToken { get; set; } = null!;
    
    public string? ApiUrlOverride { get; set; }
}