using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CountTogether.PublicApiSdk.Abstractions.Client;
using CountTogether.PublicApiSdk.Models;
using CountTogether.PublicApiSdk.Models.WebSocket;

[assembly: InternalsVisibleTo("CountTogether.PublicApiSdk.Tests")]

namespace CountTogether.PublicApiSdk.Client;

/// <inheritdoc />
public sealed class CountTogetherClient : ICountTogetherClient
{
    private const string BaseUrl = "https://developers.counttogether.app";
    private const string ApiVersionPath = "/v2";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private static readonly string LibraryVersion =
        typeof(CountTogetherClient).Assembly.GetName().Version?.ToString() ?? "1.0.0";

    private string? _apiToken;
    private string? _timeZone;
    private HttpClient? _httpClient;
    private bool _ownsHttpClient;
    private bool _configured;
    private bool _started;
    private bool _autoReconnect;

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _wsCts;
    private volatile bool _disposed;

    internal string WebSocketBaseUrl = $"wss://{new Uri(BaseUrl).Host}{ApiVersionPath}/ws";

    internal readonly ConcurrentDictionary<Guid, CacheEntry<Counter>> CounterCache = new();

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(1);

    internal static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    public event Action<Counter>? CounterUpdated;

    /// <inheritdoc />
    public event Action<Guid>? CounterDeleted;

    /// <inheritdoc />
    public event Action<Guid>? CounterMemberlistChanged;

    /// <inheritdoc />
    public async Task StartAsync(
        Action<CountTogetherClientConfiguration> configureClient, HttpClient? httpClient = null)
    {
        if (_started)
        {
            throw new InvalidOperationException("The client has already been started.");
        }

        Configure(configureClient, httpClient);

        try
        {
            await ConnectWebSocketAsync();
            _started = true;
        }
        catch
        {
            await DisconnectWebSocketAsync();
            ResetConfiguration();
            throw;
        }
    }

    internal void Configure(Action<CountTogetherClientConfiguration> configureClient, HttpClient? httpClient)
    {
        var config = new CountTogetherClientConfiguration();
        configureClient(config);

        if (string.IsNullOrWhiteSpace(config.ApiToken))
        {
            throw new ArgumentException("API token must be provided.", nameof(configureClient));
        }

        ResetConfiguration();

        _apiToken = config.ApiToken;
        _timeZone = string.IsNullOrWhiteSpace(config.TimeZone) ? null : config.TimeZone;
        _autoReconnect = config.AutoReconnect;
        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient == null;
        _configured = true;
    }

    /// <inheritdoc />
    public async Task<List<Counter>> GetCountersAsync()
    {
        EnsureConfigured();

        var hasExpired = CounterCache.Values.Any(e => e.IsExpired(CacheExpiration));
        if (CounterCache.IsEmpty || hasExpired)
        {
            await FetchAndCacheAllCountersAsync();
        }

        return CounterCache.Values.Select(e => e.Value).ToList();
    }

    /// <inheritdoc />
    public async Task<Counter> GetCounterAsync(Guid counterId)
    {
        EnsureConfigured();

        if (CounterCache.TryGetValue(counterId, out var entry) && !entry.IsExpired(CacheExpiration))
        {
            return entry.Value;
        }

        var counter = await SendAsync<Counter>(HttpMethod.Get, BuildUrl($"/counters/{counterId}", _timeZone));
        CounterCache[counterId] = new CacheEntry<Counter>(counter);
        return counter;
    }

    /// <inheritdoc />
    public Task<long> IncrementCounterAsync(Guid counterId)
    {
        EnsureConfigured();
        return SendAsync<long>(HttpMethod.Post, BuildUrl($"/counters/{counterId}/increment", null));
    }

    /// <inheritdoc />
    public Task<long> IncrementCounterAsync(Guid counterId, int amount)
    {
        EnsureValidAmount(amount);
        EnsureConfigured();
        return SendAsync<long>(HttpMethod.Post, BuildUrl($"/counters/{counterId}/increment/{amount}", null));
    }

    /// <inheritdoc />
    public Task<long> DecrementCounterAsync(Guid counterId)
    {
        EnsureConfigured();
        return SendAsync<long>(HttpMethod.Post, BuildUrl($"/counters/{counterId}/decrement", null));
    }

    /// <inheritdoc />
    public Task<long> DecrementCounterAsync(Guid counterId, int amount)
    {
        EnsureValidAmount(amount);
        EnsureConfigured();
        return SendAsync<long>(HttpMethod.Post, BuildUrl($"/counters/{counterId}/decrement/{amount}", null));
    }

    /// <inheritdoc />
    public async Task<Counter> UpdateCounterAsync(Guid counterId, UpdateCounterRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        EnsureConfigured();
        var counter = await SendAsync<Counter>(
            HttpMethod.Patch, BuildUrl($"/counters/{counterId}", _timeZone), request);
        CounterCache[counter.Id] = new CacheEntry<Counter>(counter);
        return counter;
    }

    internal static string BuildUrl(string path, string? timeZone)
    {
        var url = $"{BaseUrl}{ApiVersionPath}{path}";
        return string.IsNullOrWhiteSpace(timeZone) ? url : $"{url}?timezone={Uri.EscapeDataString(timeZone)}";
    }

    internal static Uri BuildWebSocketUri(string baseUrl, string? timeZone)
    {
        return new Uri(string.IsNullOrWhiteSpace(timeZone)
            ? baseUrl
            : $"{baseUrl}?timezone={Uri.EscapeDataString(timeZone)}");
    }

    #region WebSocket

    private async Task ConnectWebSocketAsync()
    {
        _wsCts = new CancellationTokenSource();

        _webSocket = CreateWebSocket();
        await _webSocket.ConnectAsync(BuildWebSocketUri(WebSocketBaseUrl, _timeZone), _wsCts.Token);

        await FetchAndCacheAllCountersAsync();

        var ct = _wsCts.Token;
        _ = Task.Run(() => ReceiveLoopAsync(ct));
    }

    private ClientWebSocket CreateWebSocket()
    {
        var webSocket = new ClientWebSocket();
        webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_apiToken}");
        return webSocket;
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[4096];

        while (!ct.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
        {
            try
            {
                var messageBuilder = new StringBuilder();
                WebSocketReceiveResult result;

                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await HandleDisconnectAsync(ct);
                        return;
                    }

                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                var message = messageBuilder.ToString();
                HandleMessage(message);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                return;
            }
            catch (WebSocketException)
            {
                await HandleDisconnectAsync(ct);
                return;
            }
        }
    }

    internal void HandleMessage(string message)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<JsonRpcNotification>(message, JsonSerializerOptions);
            if (notification == null)
            {
                return;
            }

            switch (notification.Method)
            {
                case "counterUpdated":
                    HandleCounterUpdated(notification.Params);
                    break;
                case "counterDeleted":
                    HandleCounterDeleted(notification.Params);
                    break;
                case "counterMemberlistChanged":
                    HandleCounterMemberlistChanged(notification.Params);
                    break;
            }
        }
        catch
        {
            // Ignore malformed messages
        }
    }

    private void HandleCounterUpdated(JsonElement? paramsElement)
    {
        if (paramsElement == null)
        {
            return;
        }

        var updatedParams = JsonSerializer.Deserialize<CounterUpdatedParams>(
            paramsElement.Value.GetRawText(), JsonSerializerOptions);
        if (updatedParams == null || updatedParams.CounterId == Guid.Empty)
        {
            return;
        }

        CounterCache.TryGetValue(updatedParams.CounterId, out var entry);
        var cached = entry?.Value;

        var counter = new Counter
        {
            Id = updatedParams.CounterId,
            Name = updatedParams.Name ?? cached?.Name ?? string.Empty,
            Type = updatedParams.Type,
            Value = updatedParams.Value ?? cached?.Value,
            StartDate = updatedParams.StartDate ?? cached?.StartDate,
            EndDate = updatedParams.EndDate ?? cached?.EndDate,
            Created = cached?.Created
        };

        CounterCache[counter.Id] = new CacheEntry<Counter>(counter);
        CounterUpdated?.Invoke(counter);
    }

    private void HandleCounterDeleted(JsonElement? paramsElement)
    {
        if (paramsElement == null)
        {
            return;
        }

        var deletedParams = JsonSerializer.Deserialize<CounterDeletedParams>(
            paramsElement.Value.GetRawText(), JsonSerializerOptions);
        if (deletedParams == null || deletedParams.CounterId == Guid.Empty)
        {
            return;
        }

        CounterCache.TryRemove(deletedParams.CounterId, out _);
        CounterDeleted?.Invoke(deletedParams.CounterId);
    }

    private void HandleCounterMemberlistChanged(JsonElement? paramsElement)
    {
        if (paramsElement == null)
        {
            return;
        }

        var changedParams = JsonSerializer.Deserialize<CounterMemberlistChangedParams>(
            paramsElement.Value.GetRawText(), JsonSerializerOptions);
        if (changedParams == null || changedParams.CounterId == Guid.Empty)
        {
            return;
        }

        CounterCache.TryRemove(changedParams.CounterId, out _);
        CounterMemberlistChanged?.Invoke(changedParams.CounterId);
    }

    private async Task HandleDisconnectAsync(CancellationToken ct)
    {
        if (_disposed || !_autoReconnect || ct.IsCancellationRequested)
        {
            return;
        }

        // Exponential back-off reconnect
        var delay = 1000; // start at 1 second
        const int maxDelay = 30000; // cap at 30 seconds

        while (!_disposed && !ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delay, ct);

                // Dispose old socket
                _webSocket?.Dispose();

                _webSocket = CreateWebSocket();
                await _webSocket.ConnectAsync(BuildWebSocketUri(WebSocketBaseUrl, _timeZone), ct);

                // Refresh cache after reconnect
                await FetchAndCacheAllCountersAsync();

                // Successfully reconnected – restart the receive loop
                _ = Task.Run(() => ReceiveLoopAsync(ct), ct);
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                delay = Math.Min(delay * 2, maxDelay);
            }
        }
    }

    private async Task DisconnectWebSocketAsync()
    {
        _wsCts?.Cancel();

        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, "Client disposing", timeoutCts.Token);
            }
            catch
            {
                // Best-effort close
            }
        }

        _webSocket?.Dispose();
        _webSocket = null;
        _wsCts?.Dispose();
        _wsCts = null;
    }

    #endregion

    #region REST helpers

    private async Task FetchAndCacheAllCountersAsync()
    {
        var counters = await SendAsync<List<Counter>>(HttpMethod.Get, BuildUrl("/counters", _timeZone));

        CounterCache.Clear();
        foreach (var counter in counters)
        {
            CounterCache[counter.Id] = new CacheEntry<Counter>(counter);
        }
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        var httpClient = _httpClient ?? throw new InvalidOperationException(
            "CountTogether API is not configured. Call StartAsync first.");

        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
        request.Headers.UserAgent.ParseAdd($"CountTogetherPublicApiSdkClient/{LibraryVersion}");
        request.Headers.Add("Library-Version", LibraryVersion);
        request.Headers.Add("Library-Platform", ".NET");
        request.Headers.Add("Library-Platform-Version", Environment.Version.ToString());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body != null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, body.GetType(), JsonSerializerOptions), Encoding.UTF8,
                "application/json");
        }

        using var timeoutCts = new CancellationTokenSource(RequestTimeout);
        using var response = await httpClient.SendAsync(request, timeoutCts.Token);
        var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateApiException(response.StatusCode, content);
        }

        return JsonSerializer.Deserialize<T>(content, JsonSerializerOptions)!;
    }

    internal static CountTogetherApiException CreateApiException(HttpStatusCode statusCode, string? body)
    {
        string? message = null;
        int? errorCode = null;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    message = GetStringProperty(root, "message")
                              ?? GetStringProperty(root, "detail")
                              ?? GetStringProperty(root, "title");
                    if (root.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.Number &&
                        code.TryGetInt32(out var parsedCode))
                    {
                        errorCode = parsedCode;
                    }
                }
                else if (root.ValueKind == JsonValueKind.String)
                {
                    message = root.GetString();
                }
            }
            catch (JsonException)
            {
                message = body.Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"The CountTogether API responded with {(int)statusCode} ({statusCode}).";
        }

        return new CountTogetherApiException(message!, statusCode, errorCode,
            string.IsNullOrEmpty(body) ? null : body);
    }

    private static string? GetStringProperty(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    #endregion

    private void EnsureConfigured()
    {
        if (!_configured)
        {
            throw new InvalidOperationException("CountTogether API is not configured. Call StartAsync first.");
        }
    }

    private static void EnsureValidAmount(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");
        }
    }

    private void ResetConfiguration()
    {
        _configured = false;
        if (_ownsHttpClient)
        {
            _httpClient?.Dispose();
        }

        _httpClient = null;
        _ownsHttpClient = false;
        _apiToken = null;
        _timeZone = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisconnectWebSocketAsync().GetAwaiter().GetResult();
        ResetConfiguration();
    }
}

internal sealed class CacheEntry<T>
{
    public T Value { get; }
    private readonly DateTime _createdAtUtc;

    public CacheEntry(T value)
    {
        Value = value;
        _createdAtUtc = DateTime.UtcNow;
    }

    public bool IsExpired(TimeSpan expiration) => DateTime.UtcNow - _createdAtUtc > expiration;
}
