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
    public void HandleMessage_CounterUpdated_FiresEvent_WhenCounterInCache()
    {
        var counterId = Guid.NewGuid().ToString();
        _client.CounterCache[counterId] = new CacheEntry<Counter>(new Counter
        {
            Id = counterId,
            DisplayName = "Old Name",
            Type = CounterType.UpDown
        });

        Counter? received = null;
        _client.CounterUpdated += c => received = c;

        var message = $$"""
        {
            "jsonrpc": "2.0",
            "method": "counterUpdated",
            "params": {
                "counterId": "{{counterId}}",
                "displayName": "New Name",
                "data": { "value": 10 }
            }
        }
        """;

        _client.HandleMessage(message);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Id, Is.EqualTo(counterId));
        Assert.That(received.DisplayName, Is.EqualTo("New Name"));
        Assert.That(received.Type, Is.EqualTo(CounterType.UpDown));
        Assert.That(received.Data, Is.Not.Null);
    }

    [Test]
    public void HandleMessage_CounterUpdated_MergesOnlyProvidedFields()
    {
        var counterId = Guid.NewGuid().ToString();
        _client.CounterCache[counterId] = new CacheEntry<Counter>(new Counter
        {
            Id = counterId,
            DisplayName = "Original Name",
            Type = CounterType.FromDate
        });

        Counter? received = null;
        _client.CounterUpdated += c => received = c;

        // Only data changes, no displayName in the event
        var message = $$"""
        {
            "jsonrpc": "2.0",
            "method": "counterUpdated",
            "params": {
                "counterId": "{{counterId}}",
                "data": { "value": 5 }
            }
        }
        """;

        _client.HandleMessage(message);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.DisplayName, Is.EqualTo("Original Name"));
        Assert.That(received.Data, Is.Not.Null);
    }

    [Test]
    public void HandleMessage_CounterUpdated_UpdatesCache()
    {
        var counterId = Guid.NewGuid().ToString();
        _client.CounterCache[counterId] = new CacheEntry<Counter>(new Counter
        {
            Id = counterId,
            DisplayName = "Old"
        });

        var message = $$"""
        {
            "jsonrpc": "2.0",
            "method": "counterUpdated",
            "params": {
                "counterId": "{{counterId}}",
                "displayName": "Updated"
            }
        }
        """;

        _client.HandleMessage(message);

        Assert.That(_client.CounterCache.ContainsKey(counterId), Is.True);
        Assert.That(_client.CounterCache[counterId].Value.DisplayName, Is.EqualTo("Updated"));
    }

    [Test]
    public void HandleMessage_CounterUpdated_CreatesPartialCounter_WhenNotInCache()
    {
        var counterId = Guid.NewGuid().ToString();

        Counter? received = null;
        _client.CounterUpdated += c => received = c;

        var message = $$"""
        {
            "jsonrpc": "2.0",
            "method": "counterUpdated",
            "params": {
                "counterId": "{{counterId}}",
                "displayName": "Brand New"
            }
        }
        """;

        _client.HandleMessage(message);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Id, Is.EqualTo(counterId));
        Assert.That(received.DisplayName, Is.EqualTo("Brand New"));
        Assert.That(_client.CounterCache.ContainsKey(counterId), Is.True);
    }

    #endregion

    #region counterDeleted

    [Test]
    public void HandleMessage_CounterDeleted_FiresEvent()
    {
        var counterId = Guid.NewGuid();
        _client.CounterCache[counterId.ToString()] = new CacheEntry<Counter>(new Counter
        {
            Id = counterId.ToString(),
            DisplayName = "To Delete"
        });

        Guid? received = null;
        _client.CounterDeleted += id => received = id;

        var message = $$"""
        {
            "jsonrpc": "2.0",
            "method": "counterDeleted",
            "params": {
                "counterId": "{{counterId}}"
            }
        }
        """;

        _client.HandleMessage(message);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Value, Is.EqualTo(counterId));
    }

    [Test]
    public void HandleMessage_CounterDeleted_RemovesFromCache()
    {
        var counterId = Guid.NewGuid();
        _client.CounterCache[counterId.ToString()] = new CacheEntry<Counter>(new Counter
        {
            Id = counterId.ToString(),
            DisplayName = "To Delete"
        });

        var message = $$"""
        {
            "jsonrpc": "2.0",
            "method": "counterDeleted",
            "params": {
                "counterId": "{{counterId}}"
            }
        }
        """;

        _client.HandleMessage(message);

        Assert.That(_client.CounterCache.ContainsKey(counterId.ToString()), Is.False);
    }

    #endregion

    #region counterMemberlistChanged

    [Test]
    public void HandleMessage_CounterMemberlistChanged_FiresEvent()
    {
        var counterId = Guid.NewGuid();

        Guid? received = null;
        _client.CounterMemberlistChanged += id => received = id;

        var message = $$"""
        {
            "jsonrpc": "2.0",
            "method": "counterMemberlistChanged",
            "params": {
                "counterId": "{{counterId}}"
            }
        }
        """;

        _client.HandleMessage(message);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Value, Is.EqualTo(counterId));
    }

    [Test]
    public void HandleMessage_CounterMemberlistChanged_InvalidatesCache()
    {
        var counterId = Guid.NewGuid();
        _client.CounterCache[counterId.ToString()] = new CacheEntry<Counter>(new Counter
        {
            Id = counterId.ToString(),
            DisplayName = "Has Members"
        });

        var message = $$"""
        {
            "jsonrpc": "2.0",
            "method": "counterMemberlistChanged",
            "params": {
                "counterId": "{{counterId}}"
            }
        }
        """;

        _client.HandleMessage(message);

        Assert.That(_client.CounterCache.ContainsKey(counterId.ToString()), Is.False);
    }

    #endregion

    #region Edge cases

    [Test]
    public void HandleMessage_MalformedJson_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _client.HandleMessage("not valid json"));
    }

    [Test]
    public void HandleMessage_EmptyJson_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _client.HandleMessage("{}"));
    }

    [Test]
    public void HandleMessage_UnknownMethod_DoesNotThrow()
    {
        var message = """
        {
            "jsonrpc": "2.0",
            "method": "unknownMethod",
            "params": {}
        }
        """;

        Assert.DoesNotThrow(() => _client.HandleMessage(message));
    }

    [Test]
    public void HandleMessage_NullParams_DoesNotFireEvents()
    {
        var updatedFired = false;
        var deletedFired = false;
        var memberlistFired = false;

        _client.CounterUpdated += _ => updatedFired = true;
        _client.CounterDeleted += _ => deletedFired = true;
        _client.CounterMemberlistChanged += _ => memberlistFired = true;

        var message = """
        {
            "jsonrpc": "2.0",
            "method": "counterUpdated"
        }
        """;

        _client.HandleMessage(message);

        Assert.That(updatedFired, Is.False);
        Assert.That(deletedFired, Is.False);
        Assert.That(memberlistFired, Is.False);
    }

    [Test]
    public void HandleMessage_NoSubscribers_DoesNotThrow()
    {
        var counterId = Guid.NewGuid().ToString();
        _client.CounterCache[counterId] = new CacheEntry<Counter>(new Counter
        {
            Id = counterId,
            DisplayName = "Test"
        });

        var message = $$"""
        {
            "jsonrpc": "2.0",
            "method": "counterUpdated",
            "params": {
                "counterId": "{{counterId}}",
                "displayName": "Updated"
            }
        }
        """;

        Assert.DoesNotThrow(() => _client.HandleMessage(message));
    }

    #endregion
}

