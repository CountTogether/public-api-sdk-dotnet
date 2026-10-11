using System.Net.WebSockets;
using CountTogether.PublicApiSdk.Client;
using CountTogether.PublicApiSdk.Models;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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
    public void IncrementDecrementWithAmount_BeforeStart_ThrowsInvalidOperationException()
    {
        using var client = new CountTogetherClient();

        Assert.ThrowsAsync<InvalidOperationException>(() => client.IncrementCounterAsync(Guid.NewGuid(), 2));
        Assert.ThrowsAsync<InvalidOperationException>(() => client.DecrementCounterAsync(Guid.NewGuid(), 2));
    }

    [Test]
    public void UpdateCounterAsync_BeforeStart_ThrowsInvalidOperationException()
    {
        using var client = new CountTogetherClient();

        Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UpdateCounterAsync(Guid.NewGuid(), new UpdateCounterRequest { Name = "Name" }));
    }

    [Test]
    public void StartAsync_WithEmptyToken_LeavesClientUnconfigured()
    {
        using var client = new CountTogetherClient();

        Assert.ThrowsAsync<ArgumentException>(() => client.StartAsync(config => { config.ApiToken = ""; }));
        Assert.ThrowsAsync<InvalidOperationException>(() => client.GetCountersAsync());
    }

    [Test]
    public void StartAsync_WhenConnectFails_RollsBackAndCanBeRetried()
    {
        using var client = new CountTogetherClient();
        client.WebSocketBaseUrl = "ws://127.0.0.1:1/v2/ws";

        Assert.CatchAsync<WebSocketException>(() => client.StartAsync(config => { config.ApiToken = "token"; }));
        Assert.ThrowsAsync<InvalidOperationException>(() => client.GetCountersAsync());

        Assert.CatchAsync<WebSocketException>(() => client.StartAsync(config => { config.ApiToken = "token"; }));
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

        Action<Counter> onUpdated = _ => { };
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

