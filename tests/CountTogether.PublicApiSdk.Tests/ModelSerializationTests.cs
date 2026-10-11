using System.Text.Json;
using CountTogether.PublicApiSdk.Client;
using CountTogether.PublicApiSdk.Models;
using CountTogether.PublicApiSdk.Models.WebSocket;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CountTogether.PublicApiSdk.Tests;

[TestFixture]
public class ModelSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = CountTogetherClient.JsonSerializerOptions;

    [Test]
    public void Counter_UpDown_Deserializes()
    {
        const string json = """
        {
            "id": "01998733-6251-731b-8631-4efe03770b43",
            "name": "My Counter",
            "type": "UpDown",
            "value": 42,
            "created": "2025-03-01T12:00:00.123456"
        }
        """;

        var counter = JsonSerializer.Deserialize<Counter>(json, JsonOptions)!;

        Assert.That(counter.Id, Is.EqualTo(Guid.Parse("01998733-6251-731b-8631-4efe03770b43")));
        Assert.That(counter.Name, Is.EqualTo("My Counter"));
        Assert.That(counter.Type, Is.EqualTo(CounterType.UpDown));
        Assert.That(counter.Value, Is.EqualTo(42));
        Assert.That(counter.StartDate, Is.Null);
        Assert.That(counter.EndDate, Is.Null);
        Assert.That(counter.Date, Is.Null);
    }

    [Test]
    public void Counter_FromDate_ExposesStartDateAsDate()
    {
        const string json = """
        {
            "id": "01998733-6251-731b-8631-4efe03770b43",
            "name": "Together",
            "type": "FromDate",
            "value": 3474,
            "startDate": "2017-04-07",
            "created": "0001-01-01T00:00:00"
        }
        """;

        var counter = JsonSerializer.Deserialize<Counter>(json, JsonOptions)!;

        Assert.That(counter.Type, Is.EqualTo(CounterType.FromDate));
        Assert.That(counter.Value, Is.EqualTo(3474));
        Assert.That(counter.StartDate, Is.EqualTo(new DateOnly(2017, 4, 7)));
        Assert.That(counter.EndDate, Is.Null);
        Assert.That(counter.Date, Is.EqualTo(new DateOnly(2017, 4, 7)));
    }

    [Test]
    public void Counter_ToDate_ExposesEndDateAsDate()
    {
        const string json = """
        {
            "id": "01998733-6251-731b-8631-4efe03770b43",
            "name": "Holiday",
            "type": "ToDate",
            "value": -1,
            "endDate": "2027-01-04",
            "created": "0001-01-01T00:00:00"
        }
        """;

        var counter = JsonSerializer.Deserialize<Counter>(json, JsonOptions)!;

        Assert.That(counter.Type, Is.EqualTo(CounterType.ToDate));
        Assert.That(counter.Value, Is.EqualTo(-1));
        Assert.That(counter.StartDate, Is.Null);
        Assert.That(counter.EndDate, Is.EqualTo(new DateOnly(2027, 1, 4)));
        Assert.That(counter.Date, Is.EqualTo(new DateOnly(2027, 1, 4)));
    }

    [Test]
    public void Counter_Created_WithoutOffset_IsUtc()
    {
        const string json = """{ "id": "01998733-6251-731b-8631-4efe03770b43", "created": "2025-03-01T12:00:00.123456" }""";

        var counter = JsonSerializer.Deserialize<Counter>(json, JsonOptions)!;

        Assert.That(counter.Created, Is.EqualTo(new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc).AddTicks(1234560)));
        Assert.That(counter.Created!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void Counter_Created_WithOffset_IsConvertedToUtc()
    {
        const string json = """{ "id": "01998733-6251-731b-8631-4efe03770b43", "created": "2025-03-01T12:00:00+02:00" }""";

        var counter = JsonSerializer.Deserialize<Counter>(json, JsonOptions)!;

        Assert.That(counter.Created, Is.EqualTo(new DateTime(2025, 3, 1, 10, 0, 0, DateTimeKind.Utc)));
        Assert.That(counter.Created!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void Counter_Created_WithZulu_IsUtc()
    {
        const string json = """{ "id": "01998733-6251-731b-8631-4efe03770b43", "created": "2025-03-01T12:00:00Z" }""";

        var counter = JsonSerializer.Deserialize<Counter>(json, JsonOptions)!;

        Assert.That(counter.Created, Is.EqualTo(new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void Counter_Created_Unset_IsNull()
    {
        const string json = """{ "id": "01998733-6251-731b-8631-4efe03770b43", "created": "0001-01-01T00:00:00" }""";

        var counter = JsonSerializer.Deserialize<Counter>(json, JsonOptions)!;

        Assert.That(counter.Created, Is.Null);
    }

    [TestCase("\"Foo\"")]
    [TestCase("\"5\"")]
    [TestCase("\"\"")]
    [TestCase("42")]
    public void Counter_UnknownType_IsUnknown(string typeJson)
    {
        var json = $$"""{ "id": "01998733-6251-731b-8631-4efe03770b43", "type": {{typeJson}}, "value": 1 }""";

        var counter = JsonSerializer.Deserialize<Counter>(json, JsonOptions)!;

        Assert.That(counter.Type, Is.EqualTo(CounterType.Unknown));
        Assert.That(counter.Date, Is.Null);
    }

    [Test]
    public void CounterList_WithUnknownType_StillDeserializes()
    {
        const string json = """
        [
            { "id": "01998733-6251-731b-8631-4efe03770b43", "name": "A", "type": "Stopwatch" },
            { "id": "0199f73d-6b5b-77bb-994f-fbbbf0107d17", "name": "B", "type": "UpDown", "value": 3 }
        ]
        """;

        var counters = JsonSerializer.Deserialize<List<Counter>>(json, JsonOptions)!;

        Assert.That(counters.Select(c => c.Type), Is.EqualTo(new[] { CounterType.Unknown, CounterType.UpDown }));
    }

    [Test]
    public void Counter_Serializes_TypeAsString_AndDatesAsIsoDate()
    {
        var counter = new Counter
        {
            Id = Guid.Parse("01998733-6251-731b-8631-4efe03770b43"),
            Name = "Holiday",
            Type = CounterType.ToDate,
            Value = 5,
            EndDate = new DateOnly(2027, 1, 4)
        };

        var json = JsonSerializer.Serialize(counter, JsonOptions);

        Assert.That(json, Does.Contain("\"type\":\"ToDate\""));
        Assert.That(json, Does.Contain("\"endDate\":\"2027-01-04\""));
        Assert.That(json, Does.Not.Contain("startDate"));
        Assert.That(json, Does.Not.Contain("\"date\""));
    }

    [Test]
    public void UpdateCounterRequest_OmitsUnsetFields()
    {
        var request = new UpdateCounterRequest { EndDate = new DateOnly(2031, 6, 15) };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions());

        Assert.That(json, Is.EqualTo("""{"endDate":"2031-06-15"}"""));
    }

    [Test]
    public void CounterUpdatedParams_Deserializes_V2Payload()
    {
        const string json = """
        {
            "counterId": "01998733-6251-731b-8631-4efe03770b43",
            "name": "Holiday",
            "type": "ToDate",
            "value": 85,
            "endDate": "2027-01-04"
        }
        """;

        var updated = JsonSerializer.Deserialize<CounterUpdatedParams>(json, JsonOptions)!;

        Assert.That(updated.CounterId, Is.EqualTo(Guid.Parse("01998733-6251-731b-8631-4efe03770b43")));
        Assert.That(updated.Name, Is.EqualTo("Holiday"));
        Assert.That(updated.Type, Is.EqualTo(CounterType.ToDate));
        Assert.That(updated.Value, Is.EqualTo(85));
        Assert.That(updated.EndDate, Is.EqualTo(new DateOnly(2027, 1, 4)));
    }

    [Test]
    public void ErrorObject_WithoutCode_HasNullCode()
    {
        var error = JsonSerializer.Deserialize<ErrorObject>("""{ "message": "Forbidden" }""", JsonOptions)!;

        Assert.That(error.Message, Is.EqualTo("Forbidden"));
        Assert.That(error.Code, Is.Null);
    }

    [Test]
    public void JsonRpcNotification_Deserializes()
    {
        const string json = """
        { "jsonrpc": "2.0", "method": "counterDeleted", "params": { "counterId": "01998733-6251-731b-8631-4efe03770b43" } }
        """;

        var notification = JsonSerializer.Deserialize<JsonRpcNotification>(json, JsonOptions)!;

        Assert.That(notification.JsonRpc, Is.EqualTo("2.0"));
        Assert.That(notification.Method, Is.EqualTo("counterDeleted"));
        Assert.That(notification.Params, Is.Not.Null);
    }
}
