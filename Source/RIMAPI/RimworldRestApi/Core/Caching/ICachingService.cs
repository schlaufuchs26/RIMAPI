// ICachingService.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RIMAPI.Models;

namespace RIMAPI.Core
{
    public enum CachePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3,
    }

    public enum CacheExpirationType
    {
        Absolute, // Expire after fixed time
        Sliding, // Reset timer on access
        Never, // Manual invalidation only
        GameTick, // Expire after N game ticks
    }

    public interface ICachingService
    {
        bool IsEnabled();

        // Basic caching
        bool TryGet<T>(string key, out T value);
        void Set<T>(
            string key,
            T value,
            TimeSpan? expiration = null,
            CachePriority priority = CachePriority.Normal
        );
        void Remove(string key);
        ApiResult<ServerCacheResponseDto> Clear();
        ApiResult<ServerCacheResponseDto> SetEnabled(bool value);

        // Property setter cache methods
        Action<TTarget, TValue> GetPropertySetter<TTarget, TValue>(string propertyName);
        Func<TTarget, TValue> GetPropertyGetter<TTarget, TValue>(string propertyName);

        // Cache-aware response generation
        Task CacheAwareResponseAsync<T>(
            HttpListenerContext context,
            string cacheKey,
            Func<Task<ApiResult<T>>> dataFactory,
            TimeSpan? expiration = null,
            CachePriority priority = CachePriority.Normal,
            CacheExpirationType expirationType = CacheExpirationType.Absolute,
            int? gameTicksExpiration = null
        );

        // Cache invalidation patterns
        void InvalidateByPattern(string pattern);
        void InvalidateByPrefix(string prefix);
        void InvalidateBySuffix(string suffix);

        // Cache statistics
        CacheStatistics GetStatistics(bool includeDetails = false);
        void Trim(CachePriority? priority = null);
    }

    public class CacheEntryDetail
    {
        [ParameterDescription("The request the cached response answers, as the cache keys it.")]
        public string Key { get; set; }

        [ParameterDescription("The C# type of the cached value.")]
        public string Type { get; set; } // Type of cached value

        [ParameterDescription("When the entry was written (UTC).")]
        public DateTime Created { get; set; }

        [ParameterDescription("When the entry was last served (UTC).")]
        public DateTime LastAccessed { get; set; }

        [ParameterDescription("When the entry expires by wall-clock time; null for another expiry rule.")]
        public DateTime? AbsoluteExpiration { get; set; }

        [ParameterDescription("Seconds until the entry expires under its own rule.")]
        public double RemainingSeconds { get; set; } // Time until expiration

        [ParameterDescription("How often the entry was served.")]
        public int Hits { get; set; } // Track hits per entry if possible, or just global hits

        [ParameterDescription("Rough size of the cached value, in bytes.")]
        public long EstimatedSize { get; set; }

        [ParameterDescription("The priority the entry was cached with: Low, Normal, High or Critical.")]
        public string Priority { get; set; }
    }

    public class RecentHitDetail
    {
        [ParameterDescription("The request the cache looked at.")]
        public string Key { get; set; }

        [ParameterDescription("When the request arrived (UTC).")]
        public DateTime Timestamp { get; set; }

        [ParameterDescription("True when a cached response answered it, false when it was generated.")]
        public bool IsHit { get; set; } // True = Hit, False = Miss/Generated
    }

    public class CacheStatistics
    {
        [ParameterDescription("How many responses the cache holds right now.")]
        public int TotalEntries { get; set; }

        [ParameterDescription("How many requests a cached response answered since the game started.")]
        public int Hits { get; set; }

        [ParameterDescription("How many requests had to be generated because nothing was cached.")]
        public int Misses { get; set; }

        [ParameterDescription("Rough memory the cached responses occupy, in bytes.")]
        public long MemoryUsageBytes { get; set; }

        [ParameterDescription("When the cache last dropped expired entries (UTC).")]
        public DateTime LastCleanup { get; set; }

        [ParameterDescription("Hits divided by hits plus misses; 0 while nothing was requested.")]
        public double HitRatio => TotalEntries > 0 ? (double)Hits / (Hits + Misses) : 0;

        [ParameterDescription("How many property accessors the cache compiled.")]
        public int CompiledDelegateCount { get; set; }

        [ParameterDescription("How often a compiled property accessor was used.")]
        public int CompiledDelegateHits { get; set; }

        [ParameterDescription("How often a property accessor was compiled instead of reused.")]
        public int CompiledDelegateMisses { get; set; }

        // New detailed data
        [ParameterDescription("One row per cached response; empty unless the request asked for details.")]
        public List<CacheEntryDetail> Entries { get; set; }

        [ParameterDescription("The most recent cache requests, newest last.")]
        public List<RecentHitDetail> RecentActivity { get; set; }
    }
}
