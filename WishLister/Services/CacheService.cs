using System.Text.Json;
using WishLister.Utils;
using System.Collections.Concurrent; 

namespace WishLister.Services;
public class CacheService
{
    private readonly ConcurrentDictionary<string, (object value, DateTime expiry)> _inMemoryCache;

    public CacheService()
    {
        _inMemoryCache = new ConcurrentDictionary<string, (object, DateTime)>();
    }


    private bool TryGetFromInMemory<T>(string key, out T? value)
    {
        value = default;
        if (_inMemoryCache.TryGetValue(key, out var cached))
        {
            if (cached.expiry > DateTime.UtcNow)
            {
                value = (T)cached.value;
                return true;
            }
            else
            {
                _inMemoryCache.TryRemove(key, out _);
            }
        }
        return false;
    }


    private void SetToInMemory<T>(string key, T value, TimeSpan? expiry = null)
    {
        var expiryTime = expiry.HasValue ? DateTime.UtcNow + expiry.Value : DateTime.MaxValue;
        _inMemoryCache.AddOrUpdate(key, (value, expiryTime), (k, v) => (value, expiryTime));
    }


    public async Task<T?> GetAsync<T>(string key)
    {
        if (TryGetFromInMemory(key, out T? inMemoryResult))
        {
            return inMemoryResult;
        }
        return default; 
    }


    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        SetToInMemory(key, value, expiry);
    }

  
    public async Task<IEnumerable<string>> GetKeysAsync(string pattern)
    {
        var keys = new List<string>();
        var now = DateTime.UtcNow;
        foreach (var kvp in _inMemoryCache)
        {
            if (kvp.Key.StartsWith(pattern.Replace("*", "")) && kvp.Value.expiry > now)
            {
                keys.Add(kvp.Key);
            }
        }
        return keys;
    }

    public async Task ClearCacheByPatternAsync(string pattern)
    {
        var keys = await GetKeysAsync(pattern);
        foreach (var key in keys)
        {
            _inMemoryCache.TryRemove(key, out _);
        }
    }

    public async Task CacheUserDataAsync(int userId, object userData)
    {
        var key = $"user:{userId}";
        await SetAsync(key, userData, TimeSpan.FromMinutes(30));
    }

    public async Task<object?> GetCachedUserDataAsync(int userId)
    {
        var key = $"user:{userId}";
        return await GetAsync<object>(key);
    }

    public async Task CacheWishlistAsync(int wishlistId, object wishlistData)
    {
        var key = $"wishlist:{wishlistId}";
        await SetAsync(key, wishlistData, TimeSpan.FromMinutes(15));
    }

    public async Task<object?> GetCachedWishlistAsync(int wishlistId)
    {
        var key = $"wishlist:{wishlistId}";
        return await GetAsync<object>(key);
    }

    public async Task CacheThemeAsync(int themeId, object themeData)
    {
        var key = $"theme:{themeId}";
        await SetAsync(key, themeData, TimeSpan.FromHours(1));
    }

    public async Task<object?> GetCachedThemeAsync(int themeId)
    {
        var key = $"theme:{themeId}";
        return await GetAsync<object>(key);
    }
}