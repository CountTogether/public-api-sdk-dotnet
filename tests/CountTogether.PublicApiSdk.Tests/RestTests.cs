using System.Net;
using System.Text;
using CountTogether.PublicApiSdk.Abstractions.Client;
using CountTogether.PublicApiSdk.Client;
using CountTogether.PublicApiSdk.Models;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CountTogether.PublicApiSdk.Tests;

[TestFixture]
public class RestTests
{
    private static readonly Guid CounterId = Guid.Parse("019f0426-895d-76bd-8ba2-7d4d28789845");

    private FakeHttpMessageHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private CountTogetherClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(_handler);
        _client = new CountTogetherClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _httpClient.Dispose();
        _handler.Dispose();
    }

    private void Configure(string? timeZone = "Europe/Berlin")
    {
        _client.Configure(config =>
        {
            config.ApiToken = "test-token";
            config.TimeZone = timeZone;
        }, _httpClient);
    }

    [Test]
    public async Task GetCountersAsync_CallsV2WithTimeZone_AndParsesAllTypes()
    {
        Configure();
        _handler.Respond(HttpStatusCode.OK, """
        [
            { "id": "01998733-6251-731b-8631-4efe03770b43", "name": "Holiday", "type": "ToDate", "value": 85, "endDate": "2027-01-04", "created": "0001-01-01T00:00:00" },
            { "id": "0199f73d-6b5b-77bb-994f-fbbbf0107d17", "name": "Coffee", "type": "UpDown", "value": 45, "created": "0001-01-01T00:00:00" },
            { "id": "01998733-6251-731b-8631-4efe03770b44", "name": "Together", "type": "FromDate", "value": 3474, "startDate": "2017-04-07", "created": "0001-01-01T00:00:00" },
            { "id": "01998733-6251-731b-8631-4efe03770b45", "name": "Future", "type": "Stopwatch" }
        ]
        """);

        var counters = await _client.GetCountersAsync();

        Assert.That(_handler.Requests, Has.Count.EqualTo(1));
        Assert.That(_handler.Requests[0].Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(_handler.Requests[0].Uri,
            Is.EqualTo("https://developers.counttogether.app/v2/counters?timezone=Europe%2FBerlin"));
        Assert.That(counters, Has.Count.EqualTo(4));
        Assert.That(counters.Single(c => c.Name == "Holiday").Date, Is.EqualTo(new DateOnly(2027, 1, 4)));
        Assert.That(counters.Single(c => c.Name == "Together").Date, Is.EqualTo(new DateOnly(2017, 4, 7)));
        Assert.That(counters.Single(c => c.Name == "Coffee").Value, Is.EqualTo(45));
        Assert.That(counters.Single(c => c.Name == "Future").Type, Is.EqualTo(CounterType.Unknown));
    }

    [Test]
    public async Task GetCountersAsync_WithoutTimeZone_OmitsQuery()
    {
        Configure(timeZone: null);
        _handler.Respond(HttpStatusCode.OK, "[]");

        await _client.GetCountersAsync();

        Assert.That(_handler.Requests[0].Uri, Is.EqualTo("https://developers.counttogether.app/v2/counters"));
    }

    [Test]
    public async Task GetCounterAsync_CallsV2SingleEndpoint()
    {
        Configure();
        _handler.Respond(HttpStatusCode.OK,
            $$"""{ "id": "{{CounterId}}", "name": "Test", "type": "UpDown", "value": 157 }""");

        var counter = await _client.GetCounterAsync(CounterId);

        Assert.That(_handler.Requests[0].Uri,
            Is.EqualTo($"https://developers.counttogether.app/v2/counters/{CounterId}?timezone=Europe%2FBerlin"));
        Assert.That(counter.Value, Is.EqualTo(157));
    }

    [TestCase(false, null, "increment")]
    [TestCase(false, 5, "increment/5")]
    [TestCase(true, null, "decrement")]
    [TestCase(true, 3, "decrement/3")]
    public async Task IncrementDecrement_PostsToV2Route(bool decrement, int? amount, string route)
    {
        Configure();
        _handler.Respond(HttpStatusCode.OK, "7");

        var result = (decrement, amount) switch
        {
            (false, null) => await _client.IncrementCounterAsync(CounterId),
            (false, _) => await _client.IncrementCounterAsync(CounterId, amount.Value),
            (true, null) => await _client.DecrementCounterAsync(CounterId),
            (true, _) => await _client.DecrementCounterAsync(CounterId, amount.Value)
        };

        Assert.That(result, Is.EqualTo(7));
        Assert.That(_handler.Requests[0].Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(_handler.Requests[0].Uri,
            Is.EqualTo($"https://developers.counttogether.app/v2/counters/{CounterId}/{route}"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void IncrementDecrement_WithNonPositiveAmount_ThrowsWithoutRequest(int amount)
    {
        Configure();

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _client.IncrementCounterAsync(CounterId, amount));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _client.DecrementCounterAsync(CounterId, amount));
        Assert.That(_handler.Requests, Is.Empty);
    }

    [Test]
    public async Task UpdateCounterAsync_SendsPatchWithOnlySetFields_AndCachesResult()
    {
        Configure();
        _handler.Respond(HttpStatusCode.OK,
            $$"""{ "id": "{{CounterId}}", "name": "Renamed", "type": "UpDown", "value": 157 }""");

        var counter = await _client.UpdateCounterAsync(CounterId, new UpdateCounterRequest { Name = "Renamed" });

        Assert.That(_handler.Requests[0].Method, Is.EqualTo(HttpMethod.Patch));
        Assert.That(_handler.Requests[0].Uri,
            Is.EqualTo($"https://developers.counttogether.app/v2/counters/{CounterId}?timezone=Europe%2FBerlin"));
        Assert.That(_handler.Requests[0].Body, Is.EqualTo("""{"name":"Renamed"}"""));
        Assert.That(_handler.Requests[0].ContentType, Is.EqualTo("application/json"));
        Assert.That(counter.Name, Is.EqualTo("Renamed"));
        Assert.That(_client.CounterCache[CounterId].Value.Name, Is.EqualTo("Renamed"));
    }

    [Test]
    public void UpdateCounterAsync_NullRequest_Throws()
    {
        Configure();

        Assert.ThrowsAsync<ArgumentNullException>(() => _client.UpdateCounterAsync(CounterId, null!));
    }

    [Test]
    public void ApiError_WithMessageAndCode_ThrowsCountTogetherApiException()
    {
        Configure();
        _handler.Respond(HttpStatusCode.NotFound,
            """{"message":"The requested resource was not found.","code":-1100}""");

        var ex = Assert.ThrowsAsync<CountTogetherApiException>(() => _client.GetCounterAsync(CounterId));

        Assert.That(ex!.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(ex.ErrorCode, Is.EqualTo(-1100));
        Assert.That(ex.Message, Is.EqualTo("The requested resource was not found."));
        Assert.That(ex, Is.InstanceOf<HttpRequestException>());
    }

    [Test]
    public void ApiError_EmptyBody_ThrowsWithStatusOnly()
    {
        Configure();
        _handler.Respond(HttpStatusCode.Unauthorized, "");

        var ex = Assert.ThrowsAsync<CountTogetherApiException>(() => _client.IncrementCounterAsync(CounterId));

        Assert.That(ex!.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(ex.ErrorCode, Is.Null);
        Assert.That(ex.ResponseBody, Is.Null);
        Assert.That(ex.Message, Does.Contain("401"));
    }

    [Test]
    public void ApiError_MessageWithoutCode_HasNullErrorCode()
    {
        Configure();
        _handler.Respond(HttpStatusCode.Forbidden, """{"message":"You are not an admin of this counter."}""");

        var ex = Assert.ThrowsAsync<CountTogetherApiException>(
            () => _client.UpdateCounterAsync(CounterId, new UpdateCounterRequest { Name = "Nope" }));

        Assert.That(ex!.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(ex.ErrorCode, Is.Null);
        Assert.That(ex.Message, Is.EqualTo("You are not an admin of this counter."));
    }

    [Test]
    public void ApiError_JsonStringBody_UsesItAsMessage()
    {
        Configure();
        _handler.Respond(HttpStatusCode.BadRequest, "\"Amount must be greater than zero.\"");

        var ex = Assert.ThrowsAsync<CountTogetherApiException>(() => _client.IncrementCounterAsync(CounterId, 1));

        Assert.That(ex!.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(ex.Message, Is.EqualTo("Amount must be greater than zero."));
    }

    [Test]
    public void ApiError_PlainTextBody_UsesItAsMessage()
    {
        Configure();
        _handler.Respond(HttpStatusCode.BadRequest, "At least one field must be provided.", "text/plain");

        var ex = Assert.ThrowsAsync<CountTogetherApiException>(
            () => _client.UpdateCounterAsync(CounterId, new UpdateCounterRequest()));

        Assert.That(ex!.Message, Is.EqualTo("At least one field must be provided."));
    }

    [Test]
    public void ApiError_ProblemDetails_UsesTitle()
    {
        Configure();
        _handler.Respond(HttpStatusCode.BadRequest,
            """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400}""",
            "application/problem+json");

        var ex = Assert.ThrowsAsync<CountTogetherApiException>(
            () => _client.UpdateCounterAsync(CounterId, new UpdateCounterRequest { Name = "x" }));

        Assert.That(ex!.Message, Is.EqualTo("One or more validation errors occurred."));
        Assert.That(ex.ErrorCode, Is.Null);
    }

    [Test]
    public async Task InjectedHttpClient_IsNotMutated_AndSurvivesDispose()
    {
        var timeout = _httpClient.Timeout;
        Configure();
        _handler.Respond(HttpStatusCode.OK, "1");

        await _client.IncrementCounterAsync(CounterId);
        await _client.IncrementCounterAsync(CounterId);
        _client.Dispose();

        Assert.That(_httpClient.DefaultRequestHeaders, Is.Empty);
        Assert.That(_httpClient.Timeout, Is.EqualTo(timeout));
        Assert.That(_handler.Requests, Has.Count.EqualTo(2));
        foreach (var request in _handler.Requests)
        {
            Assert.That(request.Authorization, Is.EqualTo("Bearer test-token"));
        }

        using var response = await _httpClient.GetAsync("https://example.invalid/still-usable");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Requests_CarryLibraryHeaders()
    {
        Configure();
        _handler.Respond(HttpStatusCode.OK, "1");

        await _client.IncrementCounterAsync(CounterId);

        var headers = _handler.Requests[0].Headers;
        Assert.That(headers["Accept"], Is.EqualTo("application/json"));
        Assert.That(headers["User-Agent"], Does.StartWith("CountTogetherPublicApiSdkClient/"));
        Assert.That(headers["Library-Platform"], Is.EqualTo(".NET"));
        Assert.That(headers.ContainsKey("Library-Version"), Is.True);
        Assert.That(headers.ContainsKey("Library-Platform-Version"), Is.True);
    }

    internal sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        private string _body = "";
        private string _mediaType = "application/json";

        public List<RecordedRequest> Requests { get; } = new();

        public void Respond(HttpStatusCode statusCode, string body, string mediaType = "application/json")
        {
            _statusCode = statusCode;
            _body = body;
            _mediaType = mediaType;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri!.AbsoluteUri,
                body,
                request.Content?.Headers.ContentType?.MediaType,
                request.Headers.Authorization?.ToString(),
                request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value))));

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, Encoding.UTF8, _mediaType)
            };
        }
    }

    internal sealed record RecordedRequest(
        HttpMethod Method,
        string Uri,
        string? Body,
        string? ContentType,
        string? Authorization,
        Dictionary<string, string> Headers);
}
