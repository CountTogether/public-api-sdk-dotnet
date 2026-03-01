using System.Text.Json;
using CountTogether.PublicApiSdk.Models;
using CountTogether.PublicApiSdk.Models.WebSocket;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CountTogether.PublicApiSdk.Tests;

[TestFixture]
public class ModelSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public void Counter_Deserializes_AllProperties()
    {
        const string json = """
        {
            "id": "01234567-89ab-cdef-0123-456789abcdef",
            "displayName": "My Counter",
            "type": "UpDown",
            "members": [
                { "userId": "user-1", "displayName": "Alice", "isAdmin": true }
            ],
            "data": { "value": 42 }
        }
        """;

        var counter = JsonSerializer.Deserialize<Counter>(json, JsonOptions)!;

        Assert.That(counter.Id, Is.EqualTo("01234567-89ab-cdef-0123-456789abcdef"));
        Assert.That(counter.DisplayName, Is.EqualTo("My Counter"));
        Assert.That(counter.Type, Is.EqualTo(CounterType.UpDown));
        Assert.That(counter.Members, Has.Count.EqualTo(1));
        Assert.That(counter.Members[0].UserId, Is.EqualTo("user-1"));
        Assert.That(counter.Members[0].DisplayName, Is.EqualTo("Alice"));
        Assert.That(counter.Members[0].IsAdmin, Is.True);
        Assert.That(counter.Data, Is.Not.Null);
    }

    [Test]
    public void Counter_Deserializes_WithNullData()
    {
        const string json = """
        {
            "id": "01234567-89ab-cdef-0123-456789abcdef",
            "displayName": "My Counter",
            "type": "FromDate",
            "members": []
        }
        """;

        var counter = JsonSerializer.Deserialize<Counter>(json, JsonOptions)!;

        Assert.That(counter.Data, Is.Null);
        Assert.That(counter.Type, Is.EqualTo(CounterType.FromDate));
        Assert.That(counter.Members, Is.Empty);
    }

    [Test]
    public void JsonRpcNotification_Deserializes_WithParams()
    {
        const string json = """
        {
            "jsonrpc": "2.0",
            "method": "counterUpdated",
            "params": { "counterId": "abc-123" }
        }
        """;

        var notification = JsonSerializer.Deserialize<JsonRpcNotification>(json, JsonOptions)!;

        Assert.That(notification.JsonRpc, Is.EqualTo("2.0"));
        Assert.That(notification.Method, Is.EqualTo("counterUpdated"));
        Assert.That(notification.Params, Is.Not.Null);
    }

    [Test]
    public void JsonRpcNotification_Deserializes_WithoutParams()
    {
        const string json = """
        {
            "jsonrpc": "2.0",
            "method": "counterDeleted"
        }
        """;

        var notification = JsonSerializer.Deserialize<JsonRpcNotification>(json, JsonOptions)!;

        Assert.That(notification.Method, Is.EqualTo("counterDeleted"));
        Assert.That(notification.Params, Is.Null);
    }

    [Test]
    public void CounterUpdatedParams_Deserializes()
    {
        const string json = """
        {
            "counterId": "abc-123",
            "displayName": "Updated Name",
            "data": { "value": 99 }
        }
        """;

        var p = JsonSerializer.Deserialize<CounterUpdatedParams>(json, JsonOptions)!;

        Assert.That(p.CounterId, Is.EqualTo("abc-123"));
        Assert.That(p.DisplayName, Is.EqualTo("Updated Name"));
        Assert.That(p.Data, Is.Not.Null);
    }

    [Test]
    public void CounterUpdatedParams_Deserializes_WithNullOptionals()
    {
        const string json = """
        {
            "counterId": "abc-123"
        }
        """;

        var p = JsonSerializer.Deserialize<CounterUpdatedParams>(json, JsonOptions)!;

        Assert.That(p.CounterId, Is.EqualTo("abc-123"));
        Assert.That(p.DisplayName, Is.Null);
        Assert.That(p.Data, Is.Null);
    }

    [Test]
    public void CounterDeletedParams_Deserializes()
    {
        const string json = """
        {
            "counterId": "abc-123"
        }
        """;

        var p = JsonSerializer.Deserialize<CounterDeletedParams>(json, JsonOptions)!;

        Assert.That(p.CounterId, Is.EqualTo("abc-123"));
    }

    [Test]
    public void CounterMemberlistChangedParams_Deserializes()
    {
        const string json = """
        {
            "counterId": "abc-123"
        }
        """;

        var p = JsonSerializer.Deserialize<CounterMemberlistChangedParams>(json, JsonOptions)!;

        Assert.That(p.CounterId, Is.EqualTo("abc-123"));
    }

    [Test]
    public void CounterMember_Deserializes()
    {
        const string json = """
        {
            "userId": "user-42",
            "displayName": "Bob",
            "isAdmin": false
        }
        """;

        var member = JsonSerializer.Deserialize<CounterMember>(json, JsonOptions)!;

        Assert.That(member.UserId, Is.EqualTo("user-42"));
        Assert.That(member.DisplayName, Is.EqualTo("Bob"));
        Assert.That(member.IsAdmin, Is.False);
    }
}

