using System.Text.Json;
using System.Text.Json.Serialization;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.Management.Files;

namespace Konnect.Management.Files
{
    public class FilePreferences : IFilePreferences
    {
        private readonly string _cachePath;
        private readonly Dictionary<string, FilePreferenceEntry> _cache;

        public FilePreferences(string cachePath)
        {
            _cachePath = cachePath;
            _cache = LoadCache() ?? [];
        }

        public string[] GetPaths()
        {
            return [.. _cache.Keys];
        }

        public FilePreferenceEntry? GetOrDefault(string fullPath)
        {
            if (_cache.TryGetValue(fullPath, out FilePreferenceEntry entry))
                return entry;

            return null;
        }

        public void Set(string fullPath, FilePreferenceEntry entry)
        {
            _cache[fullPath] = entry;
            PersistCache(_cache);
        }

        public void Remove(string filePath)
        {
            if (_cache.Remove(filePath))
                PersistCache(_cache);
        }

        private Dictionary<string, FilePreferenceEntry>? LoadCache()
        {
            if (!File.Exists(_cachePath))
                return null;

            try
            {
                using Stream fileStream = File.OpenRead(_cachePath);
                return JsonSerializer.Deserialize(fileStream, PreferenceDictionaryJsonSerializerContext.Default.DictionaryStringFilePreferenceEntry);
            }
            catch
            {
                return null;
            }
        }

        private void PersistCache(Dictionary<string, FilePreferenceEntry> cache)
        {
            try
            {
                using Stream fileStream = File.Create(_cachePath);
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
