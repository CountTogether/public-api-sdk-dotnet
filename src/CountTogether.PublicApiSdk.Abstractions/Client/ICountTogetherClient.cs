using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CountTogether.PublicApiSdk.Models;

namespace CountTogether.PublicApiSdk.Abstractions.Client;

public interface ICountTogetherClient : IDisposable
{
    /// <summary>
    /// Starts the client by applying the provided configuration. This method must be called before using any other client functionality.
    /// </summary>
    Task StartAsync(Action<CountTogetherClientConfiguration> configureClient, HttpClient? httpClient = null);

    /// <summary>
    /// Gets triggered whenever a counter is updated (incremented or decremented) by any member. The updated counter object is provided as an argument.
    /// </summary>
    event Action<Counter> CounterUpdated;
    
    /// <summary>
    /// Gets triggered whenever a counter is deleted by any member. The id of the deleted counter is provided as an argument.
    /// </summary>
    event Action<Guid> CounterDeleted;
    
    /// <summary>
    /// Gets triggered whenever the membership list of a counter changes (members added or removed). The id of the affected counter is provided as an argument.
    /// </summary>
    event Action<Guid> CounterMemberlistChanged;
    
    /// <summary>
    /// Retrieves the full list of counters. This method is typically used to fetch the initial state after connecting, or to refresh the state on demand.
    /// </summary>
    Task<List<Counter>> GetCountersAsync();
    
    /// <summary>
    /// Retrieves a specific counter by its id. This can be used to get the latest state of a counter after receiving an update event, or to fetch details of a specific counter on demand.
    /// </summary>
    /// <param name="counterId"></param>
    /// <returns></returns>
    Task<Counter> GetCounterAsync(Guid counterId);
    
    /// <summary>
    /// Increments the specified counter by 1. This method sends a request to the server to update the counter, which will then trigger a CounterUpdated event for all connected clients (including the one that made the request).
    /// </summary>
    Task<long> IncrementCounterAsync(Guid counterId);
    
    /// <summary>
    /// Decrements the specified counter by 1. This method sends a request to the server to update the counter, which will then trigger a CounterUpdated event for all connected clients (including the one that made the request).
    /// </summary>
    Task<long> DecrementCounterAsync(Guid counterId);
}