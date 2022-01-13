using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ImGui.Forms.Localization;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.IO;
using Newtonsoft.Json;

namespace Kuriimu2.ImGui.Resources
{
    static class LocalizationResources
    {
        private static readonly Lazy<Localizer> _lazy = new Lazy<Localizer>(new Localizer());
        public static ILocalizer Instance => _lazy.Value;

        #region Support methods

        public static IEnumerable<string> GetLocales()
        {
            return _lazy.Value.GetLocales();
        }

        public static string GetLanguageName(string locale)
        {
            return _lazy.Value.GetLanguageName(locale);
        }

        public static string GetLocaleByName(string name)
        {
            return _lazy.Value.GetLocaleByName(name);
        }

        #endregion

        #region Resource Instances

        public static string FileResource() => Instance.Localize("File");
        public static string ToolsResource() => Instance.Localize("Tools");
        public static string CiphersResource() => Instance.Localize("Ciphers");
        public static string CompressionsResource() => Instance.Localize("Compressions");
        public static string SettingsResource() => Instance.Localize("Settings");

        public static string OpenResource() => Instance.Localize("Open");
        public static string OpenWithResource() => Instance.Localize("OpenWith");
        public static string SaveAllResource() => Instance.Localize("SaveAll");

        public static string BatchExtractorResource() => Instance.Localize("BatchExtractor");
        public static string BatchInjectorResource() => Instance.Localize("BatchInjector");
        public static string TextSequenceSearcherResource() => Instance.Localize("TextSequenceSearcher");
        public static string HashesResource() => Instance.Localize("Hashes");
        public static string RawImageViewerResource() => Instance.Localize("RawImageViewer");

        public static string EncryptResource() => Instance.Localize("Encrypt");
        public static string DecryptResource() => Instance.Localize("Decrypt");

        public static string CompressResource() => Instance.Localize("Compress");
        public static string DecompressResource() => Instance.Localize("Decompress");

        public static string IncludeDevBuildsResource() => Instance.Localize("IncludeDevBuilds");
        public static string ChangeLanguageResource() => Instance.Localize("ChangeLanguage");
        public static string ChangeThemeResource() => Instance.Localize("ChangeTheme");

        public static string ThemeDarkResource() => Instance.Localize("ThemeDark");
        public static string ThemeLightResource() => Instance.Localize("ThemeLight");

        public static string UpdateAvailableResource(string version, string build, string remoteVersion, string remoteBuild)
            => Instance.Localize("UpdateAvailable", version, build, remoteVersion, remoteBuild);
        public static string UpdateAvailableCaptionResource() => Instance.Localize("UpdateAvailableCaption");

        public static string UnsupportedOperatingSystemExceptionResource(string os) => Instance.Localize("UnsupportedOperatingSystemException", os);

        public static string FollowingPluginsNotLoadedResource() => Instance.Localize("FollowingPluginsNotLoaded");
        public static string PluginsNotAvailableCaptionResource() => Instance.Localize("PluginsNotAvailableCaption");

        public static string ChangeLanguageTitleResource() => Instance.Localize("ChangeLanguageTitle");
        public static string ChangeLanguageCaptionResource() => Instance.Localize("ChangeLanguageCaption");

        public static string UnsavedChangesCaptionResource() => Instance.Localize("UnsavedChangesCaption");
        public static string UnsavedChangesGenericResource() => Instance.Localize("UnsavedChangesGeneric");
        public static string UnsavedChangesToFileResource(UPath path) => Instance.Localize("UnsavedChangesToFile", path);

        public static string DependantFilesCaptionResource() => Instance.Localize("DependantFilesCaption");
        public static string DependantFilesResource() => Instance.Localize("DependantFiles");

        public static string OperationsStillRunningStatusResource() => Instance.Localize("OperationsStillRunningStatus");

        public static string FileAlreadySavingStatusResource(UPath path) => Instance.Localize("FileAlreadySavingStatus", path);
        public static string FileNotSavedSuccessfullyStatusResource() => Instance.Localize("FileNotSavedSuccessfullyStatus");
        public static string FileSavedSuccessfullyStatusResource() => Instance.Localize("FileSavedSuccessfullyStatus");
        public static string SaveErrorCaptionResource() => Instance.Localize("SaveErrorCaption");

        public static string FileAlreadyOpeningStatusResource(UPath path) => Instance.Localize("FileAlreadyOpeningStatus", path);
        public static string LoadErrorCaptionResource() => Instance.Localize("LoadErrorCaption");
        public static string LoadCancelledStatusResource() => Instance.Localize("LoadCancelledStatus");
        public static string NoPluginSelectedStatusResource() => Instance.Localize("NoPluginSelectedStatus");
        public static string UnknownPluginStateResource(IPluginState state) => Instance.Localize("UnknownPluginState", state.GetType().Name);

        public static string SelectedFileInvalidResource() => Instance.Localize("SelectedFileInvalid");
        public static string NoFileSelectedStatusResource() => Instance.Localize("NoFileSelectedStatus");

        public static string AllFilesFilterResource() => Instance.Localize("AllFilesFilter");

        public static string ExceptionCatchedCaptionResource() => Instance.Localize("ExceptionCatchedCaption");

        public static string ChoosePluginTitleResource() => Instance.Localize("ChoosePluginTitle");
        public static string ChooseOpenFilePluginResource() => Instance.Localize("ChooseOpenFilePlugin");
        public static string MultiplePluginMatchesSelectionResource() => Instance.Localize("MultiplePluginMatchesSelection");
        public static string NonIdentifiablePluginSelectionResource() => Instance.Localize("NonIdentifiablePluginSelection");
        public static string NonIdentifiablePluginSelectionNoteResource() => Instance.Localize("NonIdentifiablePluginSelectionNote");
        public static string ChoosePluginContinueResource() => Instance.Localize("ChoosePluginContinue");
        public static string ChoosePluginRawBytesResource() => Instance.Localize("ChoosePluginRawBytes");
        public static string ChoosePluginCancelResource() => Instance.Localize("ChoosePluginCancel");
        public static string ChoosePluginShowAllResource() => Instance.Localize("ChoosePluginShowAll");

        public static string PluginNameColumnResource() => Instance.Localize("PluginNameColumn");
        public static string PluginTypeColumnResource() => Instance.Localize("PluginTypeColumn");
        public static string PluginDescriptionColumnResource() => Instance.Localize("PluginDescriptionColumn");
        public static string PluginIdColumnResource() => Instance.Localize("PluginIdColumn");

        // Archive Form
        public static string FileCountCaptionResource(int count) => Instance.Localize("FileCount", count);
        public static string SearchCaptionResource() => Instance.Localize("SearchPlaceholder");

        public static string FileNameCaptionResource() => Instance.Localize("FileName");
        public static string FileSizeCaptionResource() => Instance.Localize("FileSize");

        public static string ExtractCaptionResource() => Instance.Localize("Extract");
        public static string ReplaceCaptionResource() => Instance.Localize("Replace");
        public static string RenameCaptionResource() => Instance.Localize("Rename");
        public static string DeleteCaptionResource() => Instance.Localize("Delete");
        public static string AddCaptionResource() => Instance.Localize("Add");

        public static string FileNotSuccessfullyLoadedCaptionResource() => Instance.Localize("FileNotSuccessfullyLoaded");
        public static string FileNotSuccessfullyLoadedWithPluginCaptionResource(Guid id) => Instance.Localize("FileNotSuccessfullyLoadedWithPlugin", id);

        public static string NoNameGivenStatusResource() => Instance.Localize("NoNameGivenStatus");
        public static string RenameFileTitleResource() => Instance.Localize("RenameFileTitle");
        public static string RenameDirectoryTitleResource() => Instance.Localize("RenameDirectoryTitle");
        public static string RenameItemCaptionResource(string file) => Instance.Localize("RenameItemCaption", file);

        public static string NoTargetSelectedStatusResource() => Instance.Localize("NoTargetSelectedStatus");
        public static string NoFilesToExtractStatusResource() => Instance.Localize("NoFilesToExtractStatus");
        public static string NoFilesToReplaceStatusResource() => Instance.Localize("NoFilesToReplaceStatus");
        public static string NoFilesToRenameStatusResource() => Instance.Localize("NoFilesToRenameStatus");
        public static string NoFilesToAddStatusResource() => Instance.Localize("NoFilesToAddStatus");
        public static string NoFilesToDeleteStatusResource() => Instance.Localize("NoFilesToDeleteStatus");

        public static string ExtractFileProgressResource() => Instance.Localize("ExtractFileProgress");
        public static string ExtractFileCancelledStatusResource() => Instance.Localize("ExtractFileCancelledStatus");
        public static string ExtractFileSuccessfulStatusResource() => Instance.Localize("ExtractFileSuccessfulStatus");

        public static string ReplaceFileProgressResource() => Instance.Localize("ReplaceFileProgress");
        public static string ReplaceFileCancelledStatusResource() => Instance.Localize("ReplaceFileCancelledStatus");
        public static string ReplaceFileSuccessfulStatusResource() => Instance.Localize("ReplaceFileSuccessfulStatus");

        public static string RenameFileProgressResource() => Instance.Localize("RenameFileProgress");
        public static string RenameFileCancelledStatusResource() => Instance.Localize("RenameFileCancelledStatus");
        public static string RenameFileSuccessfulStatusResource() => Instance.Localize("RenameFileSuccessfulStatus");

        public static string AddFileProgressResource() => Instance.Localize("AddFileProgress");
        public static string AddFileCancelledStatusResource() => Instance.Localize("AddFileCancelledStatus");
        public static string AddFileSuccessfulStatusResource() => Instance.Localize("AddFileSuccessfulStatus");
        public static string UnableToAddFilesStatusResource() => Instance.Localize("UnableToAddFilesStatus");

        public static string DeleteFileProgressResource() => Instance.Localize("DeleteFileProgress");
        public static string DeleteFileCancelledStatusResource() => Instance.Localize("DeleteFileCancelledStatus");
        public static string DeleteFileSuccessfulStatusResource() => Instance.Localize("DeleteFileSuccessfulStatus");

        public static string CancelOperationResource() => Instance.Localize("CancelOperation");

        #endregion

        #region ILocalizer implementation

        class Localizer : ILocalizer
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

            public IEnumerable<string> GetLocales()
            {
                return _localizations.Keys;
            }

            public string GetLanguageName(string locale)
            {
                if (!_localizations.ContainsKey(locale) || !_localizations[locale].ContainsKey(NameValue_))
                    return Undefined_;

                return _localizations[locale][NameValue_];
            }

            public string GetLocaleByName(string name)
            {
                foreach (var locale in GetLocales())
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
                if (string.IsNullOrEmpty(CurrentLocale) || !_localizations[CurrentLocale].ContainsKey(name))
                    return Undefined_;

                return string.Format(_localizations[CurrentLocale][name], args);
            }

            private IDictionary<string, IDictionary<string, string>> GetLocalizations()
            {
                var assembly = Assembly.GetExecutingAssembly();
                var localNames = assembly.GetManifestResourceNames().Where(n => n.StartsWith(NameSpace_));

                var result = new Dictionary<string, IDictionary<string, string>>();
                foreach (var localName in localNames)
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
                return resourceName.Replace(NameSpace_, "").Replace(".json", "");
            }
        }

        #endregion
    }
}
