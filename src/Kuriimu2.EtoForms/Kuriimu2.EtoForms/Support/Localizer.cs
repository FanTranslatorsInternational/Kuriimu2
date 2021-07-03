using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace Kuriimu2.EtoForms.Support
{
    class Localizer
    {
        private const string LocalizationNameSpace_ = "Kuriimu2.EtoForms.Resources.Localizations";
        private const string DefaultLocale_ = "en";

        private const string InvalidLocale_ = "<invalid_locale>";
        private const string UndefinedKey_ = "<undefined>";

        private IDictionary<string, string> _localizations;

        public Localizer(string locale)
        {
            LoadLocalization(locale);
        }

        public string GetLocalization(string name, params object[] args)
        {
            if (_localizations == null)
                return InvalidLocale_;

            if (!_localizations.ContainsKey(name))
                return UndefinedKey_;

            return string.Format(_localizations[name], args);
        }

        private void LoadLocalization(string locale)
        {
            // Get resource stream
            locale ??= DefaultLocale_;
            var locStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetResourceName(locale)) ??
                            Assembly.GetExecutingAssembly().GetManifestResourceStream(GetResourceName(DefaultLocale_));

            if (locStream == null)
            {
                _localizations = null;
                return;
            }

            // Read text from stream
            var reader = new StreamReader(locStream, Encoding.UTF8);
            var json = reader.ReadToEnd();

            // Deserialize JSON
            _localizations = JsonConvert.DeserializeObject<IDictionary<string, string>>(json);
        }

        private string GetResourceName(string locale)
        {
            return LocalizationNameSpace_ + "." + locale + ".json";
        }
    }
}
