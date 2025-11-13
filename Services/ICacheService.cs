using Microsoft.Extensions.Caching.Memory;

namespace DignaApi.Services
{
    public interface ICacheService
    {
        void Set<T>(string key, T value);
        T? Get<T>(string key);
        void Remove(string key);

        void Update<T>(string key, T value)
        {
            Remove(key);
            Set<T>(key, value);
        }
    }
}
