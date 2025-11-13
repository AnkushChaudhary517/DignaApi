using Microsoft.Extensions.Caching.Memory;

namespace DignaApi.Services
{
    public class InMemoryCache : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);
        public InMemoryCache(IMemoryCache memoryCache) {
        _cache = memoryCache;
        }

        public T? Get<T>(string key)
        {
            return _cache.TryGetValue<T>(key, out var value) ? value : default;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public void Set<T>(string key, T value)
        {
            _cache.Set(key, value, _cacheDuration);
        }
    }
}
