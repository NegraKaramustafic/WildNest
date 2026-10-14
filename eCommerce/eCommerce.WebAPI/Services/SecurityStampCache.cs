using eCommerce.Services;
using Microsoft.Extensions.Caching.Memory;

namespace eCommerce.WebAPI.Services;

public sealed class SecurityStampCache : ISecurityStampCache
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private readonly IMemoryCache _cache;

    public SecurityStampCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<string?> GetOrLoadAsync(int userId, Func<Task<string?>> load)
    {
        var key = CacheKey(userId);
        if (_cache.TryGetValue(key, out string? cached))
        {
            return cached;
        }

        var stamp = await load();
        if (stamp != null)
        {
            _cache.Set(key, stamp, CacheDuration);
        }

        return stamp;
    }

    public void Invalidate(int userId)
    {
        _cache.Remove(CacheKey(userId));
    }

    private static string CacheKey(int userId) => $"user-security-stamp:{userId}";
}
