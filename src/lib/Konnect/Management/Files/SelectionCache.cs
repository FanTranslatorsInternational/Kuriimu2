using System.Text.Json;
using Konnect.DataClasses.Management.Files;

namespace Konnect.Management.Files
{
    internal static class SelectionCache
    {
        private const string CacheName = "files.json";

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
        private static readonly Dictionary<string, SelectionCacheEntry> Cache;

        static SelectionCache()
        {
            Cache = LoadCache() ?? [];
        }

        public static SelectionCacheEntry? GetOrDefault(string fullPath)
        {
            if (Cache.TryGetValue(fullPath, out SelectionCacheEntry entry))
                return entry;

            return null;
        }

        public static void Set(string fullPath, SelectionCacheEntry entry)
        {
            Cache[fullPath] = entry;
            PersistCache(Cache);
        }

        private static Dictionary<string, SelectionCacheEntry>? LoadCache()
        {
            if (!File.Exists(CacheName))
                return null;

            try
            {
                using Stream fileStream = File.OpenRead(CacheName);
                return JsonSerializer.Deserialize<Dictionary<string, SelectionCacheEntry>>(fileStream);
            }
            catch
            {
                return null;
            }
        }

        private static void PersistCache(Dictionary<string, SelectionCacheEntry> cache)
        {
            try
            {
                using Stream fileStream = File.Create(CacheName);
                JsonSerializer.Serialize(fileStream, cache, JsonOptions);
            }
            catch
            {
                // ignored
            }
        }
    }
}
