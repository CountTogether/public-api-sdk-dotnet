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
    Console.WriteLine($"{counter.DisplayName}: {counter.Id}");
}
```

## Supported Frameworks

- .NET 10.0
- .NET 9.0
- .NET Standard 2.1

## License

This project is licensed under the MIT License – see the [LICENSE](LICENSE) file for details.
