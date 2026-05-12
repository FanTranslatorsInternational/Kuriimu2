using ImGui.Forms.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kuriimu2.ImGui.Resources
{
    internal class Localizer : BaseLocalizer
    {
        private const string LocalizationFolder_ = "resources/langs";

        protected override string DefaultLocale => "en";
        protected override string UndefinedValue => "<undefined>";

        public Localizer()
        {
            Initialize();
        }

        protected override IList<LanguageInfo> InitializeLocalizations()
        {
            var result = new List<LanguageInfo>();

            string applicationDirectory = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? string.Empty;
            string localeDirectory = Path.Combine(applicationDirectory, LocalizationFolder_);

            if (!Directory.Exists(localeDirectory))
                return result;

            string[] localeFiles = Directory.GetFiles(localeDirectory);

            foreach (string localeFile in localeFiles)
            {
                // Read text from stream
                string json = File.ReadAllText(localeFile);

                // Deserialize JSON
                var translations = JsonSerializer.Deserialize(json, DictionaryJsonSerializerContext.Default.DictionaryStringString);
                if (translations is null || !translations.TryGetValue("Name", out string? localeName))
                    continue;

                result.Add(new LanguageInfo(GetLocale(localeFile), localeName, translations));
            }

            return result;
        }

        private static string GetLocale(string localeFile)
        {
            return Path.GetFileNameWithoutExtension(localeFile);
        }

        protected override string InitializeLocale()
        {
            return SettingsResources.Locale;
        }
    }

    [JsonSourceGenerationOptions(ReadCommentHandling = JsonCommentHandling.Skip)]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    public partial class DictionaryJsonSerializerContext : JsonSerializerContext;
}
