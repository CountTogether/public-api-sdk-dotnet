using CountTogether.PublicApiSdk.Client;
using CountTogether.PublicApiSdk.Models;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CountTogether.PublicApiSdk.Tests;

[TestFixture]
public class HandleMessageTests
{
    private CountTogetherClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = new CountTogetherClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
    }

    #region counterUpdated

    [Test]
    public void HandleMessage_CounterUpdated_UpDown_ReplacesCachedCounter()
    {
        var counterId = Guid.NewGuid();
        var created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var original = new Counter
        {
            Id = counterId, Name = "Old Name", Type = CounterType.UpDown, Value = 1, Created = created
        };
        _client.CounterCache[counterId] = new CacheEntry<Counter>(original);

        Counter? received = null;
        _client.CounterUpdated += c => received = c;

        _client.HandleMessage($$"""
        {
            "jsonrpc": "2.0",
            "method": "counterUpdated",
            "params": { "counterId": "{{counterId}}", "name": "New Name", "type": "UpDown", "value": 10 }
        }
        """);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Id, Is.EqualTo(counterId));
        Assert.That(received.Name, Is.EqualTo("New Name"));
        Assert.That(received.Type, Is.EqualTo(CounterType.UpDown));
        Assert.That(received.Value, Is.EqualTo(10));
        Assert.That(received.Created, Is.EqualTo(created));
        Assert.That(received, Is.Not.SameAs(original));
        Assert.That(original.Value, Is.EqualTo(1));
        Assert.That(_client.CounterCache[counterId].Value, Is.SameAs(received));
    }

    [Test]
    public void HandleMessage_CounterUpdated_FromDate_CarriesStartDate()
    {
        var counterId = Guid.NewGuid();
        Counter? received = null;
        _client.CounterUpdated += c => received = c;

        _client.HandleMessage($$"""
        {
            "jsonrpc": "2.0",
            "method": "counterUpdated",
            "params": { "counterId": "{{counterId}}", "name": "Together", "type": "FromDate", "value": 548, "startDate": "2025-04-11" }
        }
        """);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Type, Is.EqualTo(CounterType.FromDate));
        Assert.That(received.Value, Is.EqualTo(548));
        Assert.That(received.Date, Is.EqualTo(new DateOnly(2025, 4, 11)));
    }

    [Test]
    public void HandleMessage_CounterUpdated_ToDate_CarriesEndDate()
    {
        var counterId = Guid.NewGuid();
        Counter? received = null;
        _client.CounterUpdated += c => received = c;

        _client.HandleMessage($$"""
        {
            "jsonrpc": "2.0",
            "method": "counterUpdated",
            "params": { "counterId": "{{counterId}}", "name": "Holiday", "type": "ToDate", "value": 85, "endDate": "2027-01-04" }
        }
        """);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Type, Is.EqualTo(CounterType.ToDate));
        Assert.That(received.Value, Is.EqualTo(85));
        Assert.That(received.Date, Is.EqualTo(new DateOnly(2027, 1, 4)));
    }

    [Test]
    public void HandleMessage_CounterUpdated_KeepsCachedDate_WhenNotSent()
    {
        var counterId = Guid.NewGuid();
        _client.CounterCache[counterId] = new CacheEntry<Counter>(new Counter
        {
            Id = counterId, Name = "Holiday", Type = CounterType.ToDate, Value = 86, EndDate = new DateOnly(2027, 1, 4)
        });

        _client.HandleMessage($$"""
        { "jsonrpc": "2.0", "method": "counterUpdated", "params": { "counterId": "{{counterId}}", "name": "Holiday", "type": "ToDate", "value": 85 } }
        """);

        var cached = _client.CounterCache[counterId].Value;
        Assert.That(cached.Value, Is.EqualTo(85));
        Assert.That(cached.EndDate, Is.EqualTo(new DateOnly(2027, 1, 4)));
    }

    [Test]
    public void HandleMessage_CounterUpdated_CreatesPartialCounter_WhenNotInCache()
    {
        var counterId = Guid.NewGuid();

        _client.HandleMessage($$"""
        { "jsonrpc": "2.0", "method": "counterUpdated", "params": { "counterId": "{{counterId}}", "name": "Brand New", "type": "UpDown", "value": 3 } }
        """);

        Assert.That(_client.CounterCache.ContainsKey(counterId), Is.True);
        var cached = _client.CounterCache[counterId].Value;
        Assert.That(cached.Name, Is.EqualTo("Brand New"));
        Assert.That(cached.Value, Is.EqualTo(3));
        Assert.That(cached.Created, Is.Null);
    }

    [Test]
    public void HandleMessage_CounterUpdated_UnknownType_StillFiresEvent()
    {
        var counterId = Guid.NewGuid();
        Counter? received = null;
        _client.CounterUpdated += c => received = c;

        _client.HandleMessage($$"""
        { "jsonrpc": "2.0", "method": "counterUpdated", "params": { "counterId": "{{counterId}}", "name": "Future", "type": "Stopwatch", "value": 7 } }
        """);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Type, Is.EqualTo(CounterType.Unknown));
        Assert.That(received.Value, Is.EqualTo(7));
    }

    #endregion

    #region counterDeleted

    [Test]
    public void HandleMessage_CounterDeleted_FiresEvent_AndRemovesFromCache()
    {
        var counterId = Guid.NewGuid();
        _client.CounterCache[counterId] = new CacheEntry<Counter>(new Counter { Id = counterId, Name = "Gone" });

        Guid? receivedId = null;
        _client.CounterDeleted += id => receivedId = id;

        _client.HandleMessage($$"""
        { "jsonrpc": "2.0", "method": "counterDeleted", "params": { "counterId": "{{counterId}}" } }
        """);

        Assert.That(receivedId, Is.EqualTo(counterId));
        Assert.That(_client.CounterCache.ContainsKey(counterId), Is.False);
    }

    #endregion

    #region counterMemberlistChanged

    [Test]
    public void HandleMessage_CounterMemberlistChanged_FiresEvent_AndInvalidatesCache()
    {
        var counterId = Guid.NewGuid();
        _client.CounterCache[counterId] = new CacheEntry<Counter>(new Counter { Id = counterId, Name = "Shared" });

        Guid? receivedId = null;
        _client.CounterMemberlistChanged += id => receivedId = id;

        _client.HandleMessage($$"""
        { "jsonrpc": "2.0", "method": "counterMemberlistChanged", "params": { "counterId": "{{counterId}}" } }
        """);

        Assert.That(receivedId, Is.EqualTo(counterId));
        Assert.That(_client.CounterCache.ContainsKey(counterId), Is.False);
    }

    #endregion

    #region robustness

    [Test]
    public void HandleMessage_MalformedJson_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _client.HandleMessage("{ not json"));
    }

    [Test]
    public void HandleMessage_EmptyJson_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _client.HandleMessage("{}"));
    }

    [Test]
    public void HandleMessage_UnknownMethod_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _client.HandleMessage(
            """{ "jsonrpc": "2.0", "method": "somethingNew", "params": {} }"""));
    }

    [Test]
    public void HandleMessage_InvalidCounterId_DoesNotFireEvents()
    {
        var fired = false;
        _client.CounterUpdated += _ => fired = true;
        _client.CounterDeleted += _ => fired = true;

        _client.HandleMessage("""{ "jsonrpc": "2.0", "method": "counterDeleted", "params": { "counterId": "not-a-guid" } }""");
        _client.HandleMessage("""{ "jsonrpc": "2.0", "method": "counterUpdated", "params": { "name": "No id" } }""");

        Assert.That(fired, Is.False);
    }

    [Test]
    public void HandleMessage_NullParams_DoesNotFireEvents()
    {
        var fired = false;
        _client.CounterUpdated += _ => fired = true;
        _client.CounterDeleted += _ => fired = true;
        _client.CounterMemberlistChanged += _ => fired = true;

        _client.HandleMessage("""{ "jsonrpc": "2.0", "method": "counterUpdated" }""");
        _client.HandleMessage("""{ "jsonrpc": "2.0", "method": "counterDeleted" }""");
        _client.HandleMessage("""{ "jsonrpc": "2.0", "method": "counterMemberlistChanged" }""");

        Assert.That(fired, Is.False);
    }

    [Test]
    public void HandleMessage_NoSubscribers_DoesNotThrow()
    {
        var counterId = Guid.NewGuid();

        Assert.DoesNotThrow(() => _client.HandleMessage($$"""
        { "jsonrpc": "2.0", "method": "counterUpdated", "params": { "counterId": "{{counterId}}", "name": "X", "type": "UpDown", "value": 1 } }
        """));
    }

    #endregion
}
