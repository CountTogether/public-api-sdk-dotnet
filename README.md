# CountTogether Public API SDK for .NET

[![NuGet](https://img.shields.io/nuget/v/CountTogether.PublicApiSdk.svg)](https://www.nuget.org/packages/CountTogether.PublicApiSdk)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Official .NET SDK for the [CountTogether](https://counttogether.app) Public API. Provides a typed client for REST and WebSocket communication.

## Packages

| Package | Description |
|---|---|
| [`CountTogether.PublicApiSdk`](https://www.nuget.org/packages/CountTogether.PublicApiSdk) | Main SDK with the `CountTogetherClient` implementation |
| [`CountTogether.PublicApiSdk.Abstractions`](https://www.nuget.org/packages/CountTogether.PublicApiSdk.Abstractions) | Interfaces and configuration types |
| [`CountTogether.PublicApiSdk.Models`](https://www.nuget.org/packages/CountTogether.PublicApiSdk.Models) | Shared model / DTO classes |

## Installation

```bash
dotnet add package CountTogether.PublicApiSdk
```

## Quick Start

```csharp
using CountTogether.PublicApiSdk.Client;

using var client = new CountTogetherClient();

await client.StartAsync(config =>
{
    config.ApiToken = "YOUR_API_TOKEN";
});

var counters = await client.GetCountersAsync();
foreach (var counter in counters)
{
    Console.WriteLine($"{counter.Name} ({counter.Type}): {counter.Value}");
}
```

The SDK uses the CountTogether Public API **v2** (REST and WebSocket).

## Counters

| Property | Description |
|---|---|
| `Id` | Counter id (`Guid`) |
| `Name` | Display name |
| `Type` | `UpDown`, `FromDate`, `ToDate`, or `Unknown` for types this SDK version does not know |
| `Value` | `UpDown`: the counter value. `FromDate`: days since the start date. `ToDate`: days until the end date (0 or negative once reached) |
| `StartDate` / `EndDate` | `DateOnly` start date of a `FromDate` counter / end date of a `ToDate` counter |
| `Date` | The calendar date of a date counter (`StartDate` or `EndDate`), `null` for other types |
| `Created` | Creation time in UTC, `null` when the API does not provide it |

## Time zone

Day values of date counters depend on the time zone. By default the SDK sends the local time zone of the machine
(`CountTogetherClientConfiguration.DetectLocalTimeZone()`). Set `config.TimeZone` to an IANA id such as
`"Europe/Berlin"`, or to `null` to use the time zone stored in the CountTogether account. An unknown id makes the API
fall back to UTC. The WebSocket connection keeps the UTC offset from the moment it connected, so day values pushed
after a daylight saving change are only correct again after a reconnect.

## Updating counters

```csharp
await client.IncrementCounterAsync(counterId);      // by the counter's step size
await client.IncrementCounterAsync(counterId, 5);   // by a custom amount (1 to 1,000,000)
await client.DecrementCounterAsync(counterId, 2);

await client.UpdateCounterAsync(counterId, new UpdateCounterRequest { Name = "New name" });
```

`UpdateCounterAsync` only sends the fields you set, and each field must fit the counter type (`Value` for `UpDown`,
`StartDate` for `FromDate`, `EndDate` for `ToDate`). Setting `Value` currently resets the counter's step size to 1.

API errors are thrown as `CountTogetherApiException` (derived from `HttpRequestException`) with `StatusCode`, the
CountTogether `ErrorCode` (for example `-1100` for not found) and the raw `ResponseBody`. When the rate limit is
exceeded the API currently answers with `503 Service Unavailable`.

## Migrating from 1.x

Version 2.0 targets Public API v2 and is a breaking change.

| 1.x | 2.0 |
|---|---|
| `Counter.Id` (`string`) | `Counter.Id` (`Guid`) |
| `Counter.DisplayName` | `Counter.Name` |
| `Counter.Data` + `PublicV1UpDownCounterData.Value` | `Counter.Value` |
| `PublicV1FromDateCounterData.Date` / `PublicV1ToDateCounterData.Date` | `Counter.StartDate` / `Counter.EndDate` (or `Counter.Date`) |
| `Counter.Members`, `CounterMember` | removed (not part of API v2); `CounterMemberlistChanged` is still raised |
| `PublicV1UpDownCounterData.Mode`, `UpDownCounterMode` | removed (not part of API v2) |
| `CounterType` values `UpDown = 0`, `FromDate = 1`, `ToDate = 2` | `Unknown = 0`, `UpDown = 1`, `FromDate = 2`, `ToDate = 3` |
| Calling `StartAsync` twice on one instance was allowed | throws `InvalidOperationException`; a failed start can be retried |
| Failed requests: `HttpRequestException` | `CountTogetherApiException` (still an `HttpRequestException`) |

## Supported Frameworks

- .NET 10.0
- .NET 9.0
- .NET 8.0
- .NET 7.0

## License

This project is licensed under the MIT License – see the [LICENSE](LICENSE) file for details.
