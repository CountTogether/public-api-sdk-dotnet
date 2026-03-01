using CountTogether.PublicApiSdk.Abstractions.Client;
using CountTogether.PublicApiSdk.Client;
using CountTogether.PublicApiSdk.Models;

namespace CountTogether.Cli;

public static class Program
{
    private static ICountTogetherClient _client = null!;
    private static List<Counter> _counters = [];

    public static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        PrintHeader();

        var apiToken = ReadApiToken();
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No API token provided. Exiting.");
            Console.ResetColor();
            return;
        }

        _client = new CountTogetherClient();

        try
        {
            await _client.StartAsync(config =>
            {
                config.ApiToken = apiToken;
            });
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Client configured successfully!\n");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to configure client: {ex.Message}");
            Console.ResetColor();
            return;
        }

        await RunMainMenuAsync();

        _client.Dispose();
        Console.WriteLine("\nGoodbye!");
    }

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║       CountTogether CLI Example          ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static string ReadApiToken()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Enter your API Token: ");
        Console.ResetColor();
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }


    private static async Task RunMainMenuAsync()
    {
        var menuItems = new[]
        {
            "📋  List all counters",
            "🔍  Get counter details",
            "➕  Increment a counter",
            "➖  Decrement a counter",
            "👀  Watch live events",
            "🚪  Exit"
        };

        var selectedIndex = 0;

        while (true)
        {
            selectedIndex = ShowMenu("Main Menu", menuItems, selectedIndex);

            switch (selectedIndex)
            {
                case 0:
                    await ListCountersAsync();
                    break;
                case 1:
                    await GetCounterDetailsAsync();
                    break;
                case 2:
                    await IncrementCounterAsync();
                    break;
                case 3:
                    await DecrementCounterAsync();
                    break;
                case 4:
                    await WatchLiveEventsAsync();
                    break;
                case 5:
                    return;
            }
        }
    }

    /// <summary>
    /// Renders an interactive menu with arrow-key navigation.
    /// Returns the index of the selected item when Enter is pressed.
    /// </summary>
    private static int ShowMenu(string title, string[] items, int preselected = 0)
    {
        var selectedIndex = preselected;

        Console.Clear();
        Console.CursorVisible = false;

        PrintHeader();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"── {title} ──");
        Console.ResetColor();

        var topLine = Console.CursorTop;

        RenderMenuItems(items, selectedIndex, topLine);

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = (selectedIndex - 1 + items.Length) % items.Length;
                    break;
                case ConsoleKey.DownArrow:
                    selectedIndex = (selectedIndex + 1) % items.Length;
                    break;
                case ConsoleKey.Enter:
                    Console.CursorVisible = true;
                    // Move cursor below the menu
                    Console.SetCursorPosition(0, topLine + items.Length);
                    Console.WriteLine();
                    return selectedIndex;
            }

            RenderMenuItems(items, selectedIndex, topLine);
        }
    }

    private static void RenderMenuItems(string[] items, int selectedIndex, int topLine)
    {
        for (var i = 0; i < items.Length; i++)
        {
            Console.SetCursorPosition(0, topLine + i);

            if (i == selectedIndex)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.Write($"  ► {items[i]}  ");
                Console.ResetColor();
                // Clear rest of line
                Console.Write(new string(' ', Math.Max(0, Console.WindowWidth - items[i].Length - 6)));
            }
            else
            {
                Console.ResetColor();
                Console.Write($"    {items[i]}  ");
                Console.Write(new string(' ', Math.Max(0, Console.WindowWidth - items[i].Length - 6)));
            }
        }
    }

    private static async Task<List<Counter>> FetchAndCacheCountersAsync()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Fetching counters...");
        Console.ResetColor();

        _counters = await _client.GetCountersAsync();
        return _counters;
    }

    private static async Task ListCountersAsync()
    {
        try
        {
            var counters = await FetchAndCacheCountersAsync();

            if (counters.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No counters found.");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nFound {counters.Count} counter(s):\n");
            Console.ResetColor();

            PrintCounterTable(counters);
        }
        catch (Exception ex)
        {
            PrintError(ex);
        }

        WaitForKey();
    }

    private static async Task GetCounterDetailsAsync()
    {
        try
        {
            var counter = await SelectCounterAsync("Select a counter to view details");
            if (counter == null)
            {
                return;
            }

            var details = await _client.GetCounterAsync(Guid.Parse(counter.Id));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n── Counter Details ──\n");
            Console.ResetColor();

            Console.WriteLine($"  ID:           {details.Id}");
            Console.WriteLine($"  Name:         {details.DisplayName}");
            Console.WriteLine($"  Type:         {details.Type}");
            Console.WriteLine($"  Members:      {details.Members.Count}");

            if (details.Members.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("\n  Members:");
                foreach (var member in details.Members)
                {
                    var adminBadge = member.IsAdmin ? " [Admin]" : "";
                    Console.WriteLine($"    • {member.DisplayName}{adminBadge}");
                }
                Console.ResetColor();
            }

            if (details.Data.HasValue)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"\n  Data:");
                Console.WriteLine($"    {details.Data.Value.GetRawText()}");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            PrintError(ex);
        }

        WaitForKey();
    }

    private static async Task IncrementCounterAsync()
    {
        try
        {
            var counter = await SelectCounterAsync("Select a counter to increment", CounterType.UpDown);
            if (counter == null)
            {
                return;
            }

            var newValue = await _client.IncrementCounterAsync(Guid.Parse(counter.Id));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ Counter \"{counter.DisplayName}\" incremented. New value: {newValue}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            PrintError(ex);
        }

        WaitForKey();
    }

    private static async Task DecrementCounterAsync()
    {
        try
        {
            var counter = await SelectCounterAsync("Select a counter to decrement", CounterType.UpDown);
            if (counter == null)
            {
                return;
            }

            var newValue = await _client.DecrementCounterAsync(Guid.Parse(counter.Id));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ Counter \"{counter.DisplayName}\" decremented. New value: {newValue}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            PrintError(ex);
        }

        WaitForKey();
    }

    private static async Task WatchLiveEventsAsync()
    {
        Console.Clear();
        PrintHeader();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("── Live Event Watcher ──\n");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Listening for WebSocket events. Press any key to stop.\n");
        Console.ResetColor();

        var eventCount = 0;

        void OnCounterUpdated(Counter counter)
        {
            Interlocked.Increment(ref eventCount);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"  [{DateTime.Now:HH:mm:ss}] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("counterUpdated");
            Console.ResetColor();
            Console.Write("  ─  ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"\"{counter.DisplayName}\"");
            Console.ResetColor();
            Console.WriteLine($"  (ID: {counter.Id[..8]}...)");

            if (counter.Data.HasValue)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"                        Data: {counter.Data.Value.GetRawText()}");
                Console.ResetColor();
            }
        }

        void OnCounterDeleted(Guid counterId)
        {
            Interlocked.Increment(ref eventCount);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"  [{DateTime.Now:HH:mm:ss}] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("counterDeleted");
            Console.ResetColor();
            Console.WriteLine($"  ─  ID: {counterId}");
        }

        void OnCounterMemberlistChanged(Guid counterId)
        {
            Interlocked.Increment(ref eventCount);
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"  [{DateTime.Now:HH:mm:ss}] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("counterMemberlistChanged");
            Console.ResetColor();
            Console.WriteLine($"  ─  ID: {counterId}");
        }

        _client.CounterUpdated += OnCounterUpdated;
        _client.CounterDeleted += OnCounterDeleted;
        _client.CounterMemberlistChanged += OnCounterMemberlistChanged;

        try
        {
            // Wait until the user presses a key
            while (!Console.KeyAvailable)
            {
                await Task.Delay(100);
            }

            // Consume the key press
            Console.ReadKey(intercept: true);
        }
        finally
        {
            _client.CounterUpdated -= OnCounterUpdated;
            _client.CounterDeleted -= OnCounterDeleted;
            _client.CounterMemberlistChanged -= OnCounterMemberlistChanged;
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\nStopped. Received {eventCount} event(s).");
        Console.ResetColor();

        WaitForKey();
    }

    /// <summary>
    /// Shows an interactive counter selection menu.
    /// Returns null if the user cancels or no counters are available.
    /// </summary>
    private static async Task<Counter?> SelectCounterAsync(string title, CounterType? filterType = null)
    {
        var allCounters = await FetchAndCacheCountersAsync();
        var counters = filterType.HasValue
            ? allCounters.Where(c => c.Type == filterType.Value).ToList()
            : allCounters;

        if (counters.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(filterType.HasValue
                ? $"No {filterType.Value} counters available."
                : "No counters available.");
            Console.ResetColor();
            WaitForKey();
            return null;
        }

        var menuItems = new string[counters.Count + 1];
        for (var i = 0; i < counters.Count; i++)
        {
            var typeIcon = counters[i].Type switch
            {
                CounterType.UpDown => "🔢",
                CounterType.FromDate => "📅",
                CounterType.ToDate => "⏳",
                _ => "❓"
            };
            menuItems[i] = $"{typeIcon}  {counters[i].DisplayName}  (ID: {counters[i].Id[..8]}...)";
        }
        menuItems[^1] = "↩  Back";

        var selected = ShowMenu(title, menuItems);

        if (selected == counters.Count)
        {
            return null;
        }

        return counters[selected];
    }

    private static void PrintCounterTable(List<Counter> counters)
    {
        // Header
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {"Type",-14} {"Name",-30} {"Members",-10} ID");
        Console.WriteLine($"  {new string('─', 14)} {new string('─', 30)} {new string('─', 10)} {new string('─', 36)}");
        Console.ResetColor();

        foreach (var counter in counters)
        {
            var typeStr = counter.Type switch
            {
                CounterType.UpDown => "🔢 UpDown",
                CounterType.FromDate => "📅 FromDate",
                CounterType.ToDate => "⏳ ToDate",
                _ => "❓ Unknown"
            };

            Console.WriteLine($"  {typeStr,-14} {counter.DisplayName,-30} {counter.Members.Count,-10} {counter.Id}");
        }

        Console.WriteLine();
    }

    private static void PrintError(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n✗ Error: {ex.Message}");
        Console.ResetColor();
    }

    private static void WaitForKey()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\nPress any key to continue...");
        Console.ResetColor();
        Console.ReadKey(intercept: true);
    }
}