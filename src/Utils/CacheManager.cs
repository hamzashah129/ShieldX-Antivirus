using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace ShieldX.Utils
{
    /// <summary>
    /// Generic caching utility with expiration and thread-safe access.
    /// </summary>
    public class CacheManager<TKey, TValue> where TKey : notnull
    {
        private class CacheEntry
        {
            public TValue Value { get; set; }
            public DateTime ExpirationTime { get; set; }
            public bool IsExpired => DateTime.UtcNow > ExpirationTime;
        }

        private readonly Dictionary<TKey, CacheEntry> _cache = new();
        private readonly object _lockObject = new();
        private readonly TimeSpan _defaultExpiration;

        public CacheManager(TimeSpan? defaultExpiration = null)
        {
            _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Gets a value from cache or computes it if not found or expired.
        /// </summary>
        public TValue GetOrCreate(TKey key, Func<TValue> factory, TimeSpan? expiration = null)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
                {
                    Log.Debug($"Cache hit for key: {key}");
                    return entry.Value;
                }

                Log.Debug($"Cache miss for key: {key}. Computing value...");
                var value = factory();
                
                _cache[key] = new CacheEntry
                {
                    Value = value,
                    ExpirationTime = DateTime.UtcNow.Add(expiration ?? _defaultExpiration)
                };

                return value;
            }
        }

        /// <summary>
        /// Gets a value from cache asynchronously or computes it if not found or expired.
        /// </summary>
        public async Task<TValue> GetOrCreateAsync(TKey key, Func<Task<TValue>> factory, TimeSpan? expiration = null)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
                {
                    Log.Debug($"Async cache hit for key: {key}");
                    return entry.Value;
                }
            }

            Log.Debug($"Async cache miss for key: {key}. Computing value...");
            var value = await factory();

            lock (_lockObject)
            {
                _cache[key] = new CacheEntry
                {
                    Value = value,
                    ExpirationTime = DateTime.UtcNow.Add(expiration ?? _defaultExpiration)
                };
            }

            return value;
        }

        /// <summary>
        /// Removes an item from cache.
        /// </summary>
        public bool Remove(TKey key)
        {
            lock (_lockObject)
            {
                return _cache.Remove(key);
            }
        }

        /// <summary>
        /// Clears all expired items from cache.
        /// </summary>
        public int PruneExpired()
        {
            lock (_lockObject)
            {
                var expiredKeys = new List<TKey>();
                foreach (var kvp in _cache)
                {
                    if (kvp.Value.IsExpired)
                        expiredKeys.Add(kvp.Key);
                }

                foreach (var key in expiredKeys)
                    _cache.Remove(key);

                Log.Information($"Cache pruned: {expiredKeys.Count} expired entries removed");
                return expiredKeys.Count;
            }
        }

        /// <summary>
        /// Clears all cache entries.
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _cache.Clear();
                Log.Information("Cache cleared");
            }
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        public (int Total, int Valid, int Expired) GetStatistics()
        {
            lock (_lockObject)
            {
                int valid = 0, expired = 0;
                foreach (var entry in _cache.Values)
                {
                    if (entry.IsExpired)
                        expired++;
                    else
                        valid++;
                }
                return (_cache.Count, valid, expired);
            }
        }
    }
}
