using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kuriimu2.ImGui.Resources
{
    public class SettingsProvider
    {
        public static readonly SettingsProvider Instance = new();

        private readonly object _lock = new();

        private readonly Dictionary<string, string> _values;

        public SettingsProvider()
        {
            _values = CreateOrReadValues();
        }

        public T Get<T>(string name, T defaultValue)
        {
            if (!_values.TryGetValue(name, out string? value))
                return defaultValue;

            Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (type.IsEnum)
                return (T)Enum.Parse(type, value);

            return (T)Convert.ChangeType(value, type);
        }

        public void Set<T>(string name, T? value)
        {
            if (value == null)
                return;

            _values[name] = $"{value}";

            Persist();
        }

        private void Persist()
        {
            string settingsPath = GetSettingsPath();
            string settingsJson = JsonSerializer.Serialize(_values, DictionaryJsonContext.Default.DictionaryStringString);

            lock (_lock)
                File.WriteAllText(settingsPath, settingsJson);
        }

        private Dictionary<string, string> CreateOrReadValues()
        {
            string settingsPath = GetSettingsPath();
            if (!File.Exists(settingsPath))
                return new Dictionary<string, string>();

            string settingsJson = File.ReadAllText(settingsPath);
            Dictionary<string, string>? jsonData = JsonSerializer.Deserialize(settingsJson, DictionaryJsonContext.Default.DictionaryStringString);
            if (jsonData == null)
                return new Dictionary<string, string>();

            return jsonData;
        }

        private string GetSettingsPath()
        {
            return Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "settings.json");
        }
    }

    [JsonSerializable(typeof(Dictionary<string, string>))]
    partial class DictionaryJsonContext : JsonSerializerContext;
}