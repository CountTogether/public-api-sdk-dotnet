using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CountTogether.PublicApiSdk.Abstractions.Client;
using CountTogether.PublicApiSdk.Models;
using CountTogether.PublicApiSdk.Models.WebSocket;

namespace CountTogether.PublicApiSdk.Client;

public sealed class CountTogetherClient : ICountTogetherClient
{
    private string _apiUrl = "https://developers.counttogether.app/v1";
    private string _wsUrl = "wss://developers.counttogether.app/v1/ws";
    private string? _apiToken;
    private HttpClient? _httpClient;
    private bool _configured;
    private bool _autoReconnect;

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _wsCts;
    private volatile bool _disposed;

    private readonly ConcurrentDictionary<string, CacheEntry<Counter>> _counterCache = new();
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(1);

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public event Action<Counter>? CounterUpdated;
    public event Action<Guid>? CounterDeleted;
    public event Action<Guid>? CounterMemberlistChanged;

    public async Task StartAsync(
        Action<CountTogetherClientConfiguration> configureClient, HttpClient? httpClient = null)
    {
        var config = new CountTogetherClientConfiguration();
        configureClient(config);

        _httpClient = httpClient;
        _apiToken = config.ApiToken;
        _autoReconnect = config.AutoReconnect;
        _configured = true;

        if (!string.IsNullOrWhiteSpace(config.ApiUrlOverride))
        {
            _apiUrl = config.ApiUrlOverride;

            // Derive WebSocket URL from the API URL override
            var wsScheme = config.ApiUrlOverride.StartsWith("https", StringComparison.OrdinalIgnoreCase)
                ? "wss"
                : "ws";
            var uri = new Uri(config.ApiUrlOverride);
            _wsUrl = $"{wsScheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath.TrimEnd('/')}/ws";
        }

        if (string.IsNullOrWhiteSpace(_apiToken))
        {
            throw new ArgumentException("API token must be provided.", nameof(configureClient));
        }

        await ConnectWebSocketAsync();
    }

    public async Task<List<Counter>> GetCountersAsync()
    {
        EnsureConfigured();

        var hasExpired = _counterCache.Values.Any(e => e.IsExpired(CacheExpiration));
        if (_counterCache.IsEmpty || hasExpired)
        {
            await FetchAndCacheAllCountersAsync();
        }

        return _counterCache.Values.Select(e => e.Value).ToList();
    }

    public async Task<Counter> GetCounterAsync(Guid counterId)
    {
        EnsureConfigured();
        var key = counterId.ToString();

        if (_counterCache.TryGetValue(key, out var entry) && !entry.IsExpired(CacheExpiration))
        {
            return entry.Value;
        }

        // Expired or missing – fetch single counter from API and update cache
        var counter = await FetchCounterAsync(counterId);
        _counterCache[key] = new CacheEntry<Counter>(counter);
        return counter;
    }

    public async Task<long> IncrementCounterAsync(Guid counterId)
    {
        EnsureConfigured();
        using var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync($"{_apiUrl}/counters/{counterId}/increment", null);
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<long>(response);
    }

    public async Task<long> DecrementCounterAsync(Guid counterId)
    {
        EnsureConfigured();
        using var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync($"{_apiUrl}/counters/{counterId}/decrement", null);
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<long>(response);
    }

    #region WebSocket

    private async Task ConnectWebSocketAsync()
    {
        _wsCts = new CancellationTokenSource();

        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_apiToken}");

        await _webSocket.ConnectAsync(new Uri(_wsUrl), _wsCts.Token);

        await FetchAndCacheAllCountersAsync();

        _ = Task.Run(() => ReceiveLoopAsync(_wsCts.Token));
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
                    result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), ct);

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

    private void HandleMessage(string message)
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
        if (updatedParams == null)
        {
            return;
        }

        var key = updatedParams.CounterId;

        if (_counterCache.TryGetValue(key, out var entry))
        {
            // Merge updated fields into the cached counter
            var counter = entry.Value;
            if (updatedParams.DisplayName != null)
            {
                counter.DisplayName = updatedParams.DisplayName;
            }

            if (updatedParams.Data != null)
            {
                counter.Data = updatedParams.Data;
            }

            _counterCache[key] = new CacheEntry<Counter>(counter);
            CounterUpdated?.Invoke(counter);
        }
        else
        {
            // Counter not in cache – build a partial object and cache it
            var counter = new Counter
            {
                Id = updatedParams.CounterId,
                DisplayName = updatedParams.DisplayName ?? string.Empty,
                Data = updatedParams.Data
            };

            _counterCache[key] = new CacheEntry<Counter>(counter);
            CounterUpdated?.Invoke(counter);
        }
    }

    private void HandleCounterDeleted(JsonElement? paramsElement)
    {
        if (paramsElement == null)
        {
            return;
        }

        var deletedParams = JsonSerializer.Deserialize<CounterDeletedParams>(
            paramsElement.Value.GetRawText(), JsonSerializerOptions);
        if (deletedParams == null)
        {
            return;
        }

        _counterCache.TryRemove(deletedParams.CounterId, out _);

        if (Guid.TryParse(deletedParams.CounterId, out var counterId))
        {
            CounterDeleted?.Invoke(counterId);
        }
    }

    private void HandleCounterMemberlistChanged(JsonElement? paramsElement)
    {
        if (paramsElement == null)
        {
            return;
        }

        var changedParams = JsonSerializer.Deserialize<CounterMemberlistChangedParams>(
            paramsElement.Value.GetRawText(), JsonSerializerOptions);
        if (changedParams == null)
        {
            return;
        }

        // Invalidate cache entry so the next access fetches fresh data including the new member list
        _counterCache.TryRemove(changedParams.CounterId, out _);

        if (Guid.TryParse(changedParams.CounterId, out var counterId))
        {
            CounterMemberlistChanged?.Invoke(counterId);
        }
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

                _webSocket = new ClientWebSocket();
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_apiToken}");

                await _webSocket.ConnectAsync(new Uri(_wsUrl), ct);

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
        using var httpClient = CreateHttpClient();
        var response = await httpClient.GetAsync($"{_apiUrl}/counters");
        response.EnsureSuccessStatusCode();
        var counters = await DeserializeResponse<List<Counter>>(response);

        _counterCache.Clear();
        foreach (var counter in counters)
        {
            _counterCache[counter.Id] = new CacheEntry<Counter>(counter);
        }
    }

    private async Task<Counter> FetchCounterAsync(Guid counterId)
    {
        using var httpClient = CreateHttpClient();
        var response = await httpClient.GetAsync($"{_apiUrl}/counters/{counterId}");
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<Counter>(response);
    }

    #endregion

    private void EnsureConfigured()
    {
        if (!_configured)
        {
            throw new InvalidOperationException("CountTogether API is not configured. Call StartAsync first.");
        }
    }

    private static async Task<T> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions)!;
    }

    private HttpClient CreateHttpClient()
    {
        var httpClient = _httpClient ?? new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "CountTogetherPublicApiSdkClient/1.0");
        httpClient.DefaultRequestHeaders.Add(
            "Library-Version", typeof(CountTogetherClient).Assembly.GetName().Version?.ToString() ?? "1.0.0");
        httpClient.DefaultRequestHeaders.Add("Library-Platform", ".NET");
        httpClient.DefaultRequestHeaders.Add("Library-Platform-Version", Environment.Version.ToString());
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.Timeout = new TimeSpan(0, 0, 30);
        return httpClient;
    }

    private void ReleaseUnmanagedResources()
    {
        _disposed = true;
        DisconnectWebSocketAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~CountTogetherClient()
    {
        ReleaseUnmanagedResources();
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
