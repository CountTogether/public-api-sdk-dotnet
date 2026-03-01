using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CountTogether.PublicApiSdk.Abstractions.Client;
using CountTogether.PublicApiSdk.Models;

namespace CountTogether.PublicApiSdk.Client;

public sealed class CountTogetherClient : ICountTogetherClient
{
    private string _apiUrl = "https://developers.counttogether.app/v1";
    private string? _apiToken;
    private HttpClient? _httpClient;
    private bool _configured = false;

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
        _configured = true;

        if (!string.IsNullOrWhiteSpace(config.ApiUrlOverride))
        {
            _apiUrl = config.ApiUrlOverride;
        }

        if (string.IsNullOrWhiteSpace(_apiToken))
        {
            throw new ArgumentException("API token must be provided.", nameof(configureClient));
        }
    }

    public async Task<List<Counter>> GetCountersAsync()
    {
        using var httpClient = CreateHttpClient();
        var response = await httpClient.GetAsync($"{_apiUrl}/counters");
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<List<Counter>>(response);
    }

    public async Task<Counter> GetCounterAsync(Guid counterId)
    {
        using var httpClient = CreateHttpClient();
        var response = await httpClient.GetAsync($"{_apiUrl}/counters/{counterId}");
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<Counter>(response);
    }

    public async Task<long> IncrementCounterAsync(Guid counterId)
    {
        using var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync($"{_apiUrl}/counters/{counterId}/increment", null);
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<long>(response);
    }

    public async Task<long> DecrementCounterAsync(Guid counterId)
    {
        if (!_configured)
        {
            throw new InvalidOperationException("CountTogether API is not configured.");
        }

        using var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync($"{_apiUrl}/counters/{counterId}/decrement", null);
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<long>(response);
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