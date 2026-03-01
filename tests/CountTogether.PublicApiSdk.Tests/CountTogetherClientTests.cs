using CountTogether.PublicApiSdk.Client;

namespace CountTogether.PublicApiSdk.Tests;

[TestFixture]
public class CountTogetherClientTests
{
    [Test]
    public void GetCountersAsync_BeforeStart_ThrowsInvalidOperationException()
    {
        using var client = new CountTogetherClient();

        Assert.ThrowsAsync<InvalidOperationException>(() => client.GetCountersAsync());
    }

    [Test]
    public void GetCounterAsync_BeforeStart_ThrowsInvalidOperationException()
    {
        using var client = new CountTogetherClient();

        Assert.ThrowsAsync<InvalidOperationException>(() => client.GetCounterAsync(Guid.NewGuid()));
    }

    [Test]
    public void IncrementCounterAsync_BeforeStart_ThrowsInvalidOperationException()
    {
        using var client = new CountTogetherClient();

        Assert.ThrowsAsync<InvalidOperationException>(() => client.IncrementCounterAsync(Guid.NewGuid()));
    }

    [Test]
    public void DecrementCounterAsync_BeforeStart_ThrowsInvalidOperationException()
    {
        using var client = new CountTogetherClient();

        Assert.ThrowsAsync<InvalidOperationException>(() => client.DecrementCounterAsync(Guid.NewGuid()));
    }

    [Test]
    public void StartAsync_WithEmptyToken_ThrowsArgumentException()
    {
        using var client = new CountTogetherClient();

        Assert.ThrowsAsync<ArgumentException>(() =>
            client.StartAsync(config => { config.ApiToken = ""; }));
    }

    [Test]
    public void StartAsync_WithWhitespaceToken_ThrowsArgumentException()
    {
        using var client = new CountTogetherClient();

        Assert.ThrowsAsync<ArgumentException>(() =>
            client.StartAsync(config => { config.ApiToken = "   "; }));
    }

    [Test]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        var client = new CountTogetherClient();

        Assert.DoesNotThrow(() =>
        {
            client.Dispose();
            client.Dispose();
        });
    }

    [Test]
    public void Events_CanSubscribeAndUnsubscribe()
    {
        using var client = new CountTogetherClient();

        Action<CountTogether.PublicApiSdk.Models.Counter> onUpdated = _ => { };
        Action<Guid> onDeleted = _ => { };
        Action<Guid> onMemberlist = _ => { };

        Assert.DoesNotThrow(() =>
        {
            client.CounterUpdated += onUpdated;
            client.CounterDeleted += onDeleted;
            client.CounterMemberlistChanged += onMemberlist;

            client.CounterUpdated -= onUpdated;
            client.CounterDeleted -= onDeleted;
            client.CounterMemberlistChanged -= onMemberlist;
        });
    }
}

