using ImGui.Forms.Models;

namespace Kuriimu2.ImGui.Resources
{
    internal class SettingsResources
    {
        private const string LastDirectoryName_ = "LastDirectory";
        private const string TypeExtensionLastDirectoryName_ = "TypeExtensionLastDirectory";
        private const string BatchInputDirectoryName_ = "BatchInputDirectory";
        private const string BatchOutputDirectoryName_ = "BatchOutputDirectory";
        private const string SequenceSearchDirectoryName_ = "SequenceSearchDirectory";
        private const string ThumbnailWidthName_ = "ThumbnailWidth";
        private const string ThumbnailHeightName_ = "ThumbnailHeight";
        private const string IncludeDevBuildsName_ = "IncludeDevBuilds";
        private const string LocaleName_ = "Locale";
        private const string ThemeName_ = "Theme";
        private const string ReplaceFontCharactersName_ = "ReplaceFontCharacters";

        public static string LastDirectory
        {
            get => SettingsProvider.Instance.Get(LastDirectoryName_, string.Empty);
            set => SettingsProvider.Instance.Set(LastDirectoryName_, value);
        }

        public static string TypeExtensionLastDirectory
        {
            get => SettingsProvider.Instance.Get(TypeExtensionLastDirectoryName_, string.Empty);
            set => SettingsProvider.Instance.Set(TypeExtensionLastDirectoryName_, value);
        }

        public static string BatchInputDirectory
        {
            get => SettingsProvider.Instance.Get(BatchInputDirectoryName_, string.Empty);
            set => SettingsProvider.Instance.Set(BatchInputDirectoryName_, value);
        }

        public static string BatchOutputDirectory
        {
            get => SettingsProvider.Instance.Get(BatchOutputDirectoryName_, string.Empty);
            set => SettingsProvider.Instance.Set(BatchOutputDirectoryName_, value);
        }

        public static string SequenceSearchDirectory
        {
            get => SettingsProvider.Instance.Get(SequenceSearchDirectoryName_, string.Empty);
            set => SettingsProvider.Instance.Set(SequenceSearchDirectoryName_, value);
        }

        public static int ThumbnailWidth
        {
            get => SettingsProvider.Instance.Get(ThumbnailWidthName_, 96);
            set => SettingsProvider.Instance.Set(ThumbnailWidthName_, value);
        }

        public static int ThumbnailHeight
        {
            get => SettingsProvider.Instance.Get(ThumbnailHeightName_, 64);
            set => SettingsProvider.Instance.Set(ThumbnailHeightName_, value);
        }

        public static bool IncludeDevBuilds
        {
            get => SettingsProvider.Instance.Get(IncludeDevBuildsName_, false);
            set => SettingsProvider.Instance.Set(IncludeDevBuildsName_, value);
        }

        public static string Locale
        {
            get => SettingsProvider.Instance.Get(LocaleName_, "en");
            set => SettingsProvider.Instance.Set(LocaleName_, value);
        }

        public static Theme Theme
        {
            get => SettingsProvider.Instance.Get(ThemeName_, Theme.Dark);
            set => SettingsProvider.Instance.Set(ThemeName_, value);
        }

        public static bool ReplaceFontCharacters
        {
            get => SettingsProvider.Instance.Get(ReplaceFontCharactersName_, true);
            set => SettingsProvider.Instance.Set(ReplaceFontCharactersName_, value);
        }
    }
}
