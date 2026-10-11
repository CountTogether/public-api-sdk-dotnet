using CountTogether.PublicApiSdk.Abstractions.Client;
using CountTogether.PublicApiSdk.Client;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CountTogether.PublicApiSdk.Tests;

[TestFixture]
public class ConfigurationTests
{
    [Test]
    public void DetectLocalTimeZone_IanaZone_ReturnsIanaId()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        Assert.That(CountTogetherClientConfiguration.DetectLocalTimeZone(zone), Is.EqualTo("Europe/Berlin"));
    }

    [Test]
    public void DetectLocalTimeZone_WindowsZone_ReturnsIanaId()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

        Assert.That(CountTogetherClientConfiguration.DetectLocalTimeZone(zone), Is.EqualTo("Europe/Berlin"));
    }

    [TestCase("Local")]
    [TestCase("Not A Real Zone")]
    public void DetectLocalTimeZone_UnresolvableZone_ReturnsNull(string id)
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone(id, TimeSpan.FromHours(1), id, id);

        Assert.That(CountTogetherClientConfiguration.DetectLocalTimeZone(zone), Is.Null);
    }

    [Test]
    public void TimeZone_DefaultsToDetectedLocalTimeZone()
    {
        var config = new CountTogetherClientConfiguration();

        Assert.That(config.TimeZone, Is.EqualTo(CountTogetherClientConfiguration.DetectLocalTimeZone()));
    }

    [Test]
    public void BuildWebSocketUri_WithTimeZone_AddsEscapedQuery()
    {
        var uri = CountTogetherClient.BuildWebSocketUri("wss://developers.counttogether.app/v2/ws", "America/New_York");

        Assert.That(uri.AbsoluteUri, Is.EqualTo("wss://developers.counttogether.app/v2/ws?timezone=America%2FNew_York"));
    }

    [Test]
    public void BuildWebSocketUri_WithoutTimeZone_HasNoQuery()
    {
        var uri = CountTogetherClient.BuildWebSocketUri("wss://developers.counttogether.app/v2/ws", null);

        Assert.That(uri.AbsoluteUri, Is.EqualTo("wss://developers.counttogether.app/v2/ws"));
    }

    [Test]
    public void DefaultWebSocketUrl_TargetsV2()
    {
        using var client = new CountTogetherClient();

        Assert.That(client.WebSocketBaseUrl, Is.EqualTo("wss://developers.counttogether.app/v2/ws"));
    }
}
