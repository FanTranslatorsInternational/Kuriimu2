using ImGui.Forms.Localization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Kuriimu2.ImGui.Resources
{
    internal class Localizer : ILocalizer
    {
        private const string NameSpace_ = "Kuriimu2.ImGui.Resources.Localizations.";
        private const string DefaultLocale_ = "en";
        private const string NameValue_ = "Name";

        private const string Undefined_ = "<undefined>";

        private readonly IDictionary<string, IDictionary<string, string>> _localizations;

        public string CurrentLocale { get; private set; } = DefaultLocale_;

        public Localizer()
        {
            // Load localizations
            _localizations = GetLocalizations();

            // Set default locale
            if (_localizations.Count == 0)
                CurrentLocale = string.Empty;
            else if (!_localizations.ContainsKey(DefaultLocale_))
                CurrentLocale = _localizations.FirstOrDefault().Key;
        }

        public IList<string> GetLocales()
        {
            return _localizations.Keys.ToArray();
        }

        public string GetLanguageName(string locale)
        {
            if (!_localizations.ContainsKey(locale) || !_localizations[locale].ContainsKey(NameValue_))
                return Undefined_;

            return _localizations[locale][NameValue_];
        }

        public string GetLocaleByName(string name)
        {
            foreach (string locale in GetLocales())
                if (GetLanguageName(locale) == name)
                    return locale;

            return Undefined_;
        }

        public void ChangeLocale(string locale)
        {
            // Do nothing, if locale was not found
            if (!_localizations.ContainsKey(locale))
                return;

            CurrentLocale = locale;
        }

        public string Localize(string name, params object[] args)
        {
            // Return localization of current locale
            if (!string.IsNullOrEmpty(CurrentLocale) && _localizations[CurrentLocale].ContainsKey(name))
                return string.Format(_localizations[CurrentLocale][name], args);

            // Otherwise, return localization of default locale
            if (!string.IsNullOrEmpty(DefaultLocale_) && _localizations[DefaultLocale_].ContainsKey(name))
                return string.Format(_localizations[DefaultLocale_][name], args);

            // Otherwise, return localization placeholder
            return Undefined_;
        }

        private IDictionary<string, IDictionary<string, string>> GetLocalizations()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var localNames = assembly.GetManifestResourceNames().Where(n => n.StartsWith(NameSpace_));

            var result = new Dictionary<string, IDictionary<string, string>>();
            foreach (string localName in localNames)
            {
                var locStream = assembly.GetManifestResourceStream(localName);
                if (locStream == null)
                    continue;

                // Read text from stream
                var reader = new StreamReader(locStream, Encoding.UTF8);
                var json = reader.ReadToEnd();

                // Deserialize JSON
                result.Add(GetLocale(localName), JsonConvert.DeserializeObject<IDictionary<string, string>>(json));
            }

            return result;
        }

        private string GetLocale(string resourceName)
        {
            return resourceName.Replace(NameSpace_, string.Empty).Replace(".json", string.Empty);
        }
    }
}
