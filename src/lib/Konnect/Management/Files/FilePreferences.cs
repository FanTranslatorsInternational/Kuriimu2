using Konnect.DataClasses.Management.Files;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Konnect.Management.Files
{
    public static class FilePreferences
    {
        private const string CacheName = "preferences.json";

        private static readonly Dictionary<string, FilePreferenceEntry> Cache;

        static FilePreferences()
        {
            Cache = LoadCache() ?? [];
        }

        public static string[] GetPaths()
        {
            return [.. Cache.Keys];
        }

        public static FilePreferenceEntry? GetOrDefault(string fullPath)
        {
            if (Cache.TryGetValue(fullPath, out FilePreferenceEntry entry))
                return entry;

            return null;
        }

        public static void Set(string fullPath, FilePreferenceEntry entry)
        {
            Cache[fullPath] = entry;
            PersistCache(Cache);
        }

        public static void Remove(string filePath)
        {
            if (Cache.Remove(filePath))
                PersistCache(Cache);
        }

        private static Dictionary<string, FilePreferenceEntry>? LoadCache()
        {
            if (!File.Exists(CacheName))
                return null;

            try
            {
                using Stream fileStream = File.OpenRead(CacheName);
                return JsonSerializer.Deserialize(fileStream, PreferenceDictionaryJsonSerializerContext.Default.DictionaryStringFilePreferenceEntry);
            }
            catch
            {
                return null;
            }
        }

        private static void PersistCache(Dictionary<string, FilePreferenceEntry> cache)
        {
            try
            {
                using Stream fileStream = File.Create(CacheName);
                JsonSerializer.Serialize(fileStream, cache, PreferenceDictionaryJsonSerializerContext.Default.DictionaryStringFilePreferenceEntry);
            }
            catch
            {
                // ignored
            }
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = false)]
    [JsonSerializable(typeof(Dictionary<string, FilePreferenceEntry>))]
    internal partial class PreferenceDictionaryJsonSerializerContext : JsonSerializerContext;
}
