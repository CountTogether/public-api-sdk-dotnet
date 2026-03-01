using CountTogether.PublicApiSdk.Client;

namespace CountTogether.PublicApiSdk.Tests;

[TestFixture]
public class CacheEntryTests
{
    [Test]
    public void NewEntry_IsNotExpired()
    {
        var entry = new CacheEntry<string>("test");

        Assert.That(entry.IsExpired(TimeSpan.FromMinutes(1)), Is.False);
    }

    [Test]
    public void NewEntry_StoresValue()
    {
        var entry = new CacheEntry<int>(42);

        Assert.That(entry.Value, Is.EqualTo(42));
    }

    [Test]
    public async Task Entry_WithZeroExpiration_IsExpired()
    {
        var entry = new CacheEntry<string>("test");

        await Task.Delay(15);

        Assert.That(entry.IsExpired(TimeSpan.Zero), Is.True);
    }

    [Test]
    public void Entry_WithLargeExpiration_IsNotExpired()
    {
        var entry = new CacheEntry<string>("test");

        Assert.That(entry.IsExpired(TimeSpan.FromHours(24)), Is.False);
    }
}

