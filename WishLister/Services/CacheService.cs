// WishLister.Services\CacheService.cs
using System.Text.Json;
using WishLister.Utils;
using System.Collections.Concurrent; // Добавляем для потокобезопасного словаря

namespace WishLister.Services;
public class CacheService
{
    // Убираем поля _redis и _db
    // private readonly IConnectionMultiplexer? _redis;
    // private readonly IDatabase? _db;

    // Оставляем только InMemory кэш
    private readonly ConcurrentDictionary<string, (object value, DateTime expiry)> _inMemoryCache;

    public CacheService()
    {
        _inMemoryCache = new ConcurrentDictionary<string, (object, DateTime)>();
        // Убираем попытку подключения к Redis
        Console.WriteLine("CacheService initialized with InMemory storage.");
    }

    // Методы GetAsync, SetAsync, RemoveAsync, KeyExistsAsync, GetKeysAsync, ClearCacheByPatternAsync
    // и специфичные методы (CacheUserDataAsync и т.д.) остаются, но работают ТОЛЬКО с _inMemoryCache
    // Примеры методов (без Redis-логики):

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
                // Удаляем просроченный ключ
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
        return default; // Не нашли нигде
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        SetToInMemory(key, value, expiry);
    }

    public async Task<bool> RemoveAsync(string key)
    {
        // Удаляем только из InMemory
        return _inMemoryCache.TryRemove(key, out _);
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        // Проверяем только InMemory
        if (_inMemoryCache.TryGetValue(key, out var cached))
        {
            if (cached.expiry > DateTime.UtcNow)
            {
                return true;
            }
            else
            {
                _inMemoryCache.TryRemove(key, out _);
            }
        }
        return false;
    }

    // GetKeysAsync и ClearCacheByPatternAsync могут быть сложнее или неэффективны
    // для InMemory кэша без дополнительной структуры данных.
    // Их можно реализовать, например, с использованием ConcurrentDictionary<string, HashSet<string>>
    // для группировки ключей по префиксу, или оставить как есть с логикой только для Redis,
    // если они не критичны.
    // Для упрощения, можно оставить их как возвращающие пустое/false,
    // или реализовать простой поиск по ключам (менее эффективно).
    public async Task<IEnumerable<string>> GetKeysAsync(string pattern)
    {
        // Простая реализация: ищем ключи, соответствующие шаблону
        // Это может быть неэффективно для большого кэша.
        var keys = new List<string>();
        var now = DateTime.UtcNow;
        foreach (var kvp in _inMemoryCache)
        {
            // Простая проверка: если ключ начинается с паттерна (без сложных масок)
            if (kvp.Key.StartsWith(pattern.Replace("*", "")) && kvp.Value.expiry > now)
            {
                keys.Add(kvp.Key);
            }
        }
        return keys;
    }

    public async Task ClearCacheByPatternAsync(string pattern)
    {
        // Простая реализация: находим и удаляем ключи по паттерну
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