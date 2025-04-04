using Microsoft.Extensions.Caching.Memory;
using OnlineJudgeAPI.Controllers;
using System;
using System.Threading.Tasks;

namespace OnlineJudgeAPI.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;

        public MemoryCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            _cache.TryGetValue(key, out T value);
            return Task.FromResult(value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            _cache.Set(key, value, expiration);
            return Task.CompletedTask;
        }
    }
}
