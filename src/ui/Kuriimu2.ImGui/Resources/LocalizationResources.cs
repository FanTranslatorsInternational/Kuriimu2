using System;
using ImGui.Forms.Localization;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Plugin.File;

namespace Kuriimu2.ImGui.Resources
{
    static class LocalizationResources
    {
        private static readonly Lazy<ILocalizer> Lazy = new(() => new Localizer());
        public static ILocalizer Instance => Lazy.Value;

        // Kuriimu2

        // Menus
        public static LocalizedString MenuFile => LocalizedString.FromId("Menu.File");
        public static LocalizedString MenuTools => LocalizedString.FromId("Menu.Tools");
        public static LocalizedString MenuCiphers => LocalizedString.FromId("Menu.Ciphers");
        public static LocalizedString MenuCompressions => LocalizedString.FromId("Menu.Compressions");
        public static LocalizedString MenuSettings => LocalizedString.FromId("Menu.Settings");
        public static LocalizedString MenuHelp => LocalizedString.FromId("Menu.Help");

        // File Menu
        public static LocalizedString MenuFileOpen => LocalizedString.FromId("Menu.File.Open");
        public static LocalizedString MenuFileOpenWith => LocalizedString.FromId("Menu.File.OpenWith");
        public static LocalizedString MenuFileSave => LocalizedString.FromId("Menu.File.Save");
        public static LocalizedString MenuFileSaveAs => LocalizedString.FromId("Menu.File.SaveAs");
        public static LocalizedString MenuFileSaveAll => LocalizedString.FromId("Menu.File.SaveAll");
        public static LocalizedString MenuFileClose => LocalizedString.FromId("Menu.File.Close");

        // Tools Menu
        public static LocalizedString MenuToolsTextSequenceSearcher => LocalizedString.FromId("Menu.Tools.TextSequenceSearcher");
        public static LocalizedString MenuToolsBatchExtractor => LocalizedString.FromId("Menu.Tools.BatchExtractor");
        public static LocalizedString MenuToolsBatchInjector => LocalizedString.FromId("Menu.Tools.BatchInjector");
        public static LocalizedString MenuToolsHashes => LocalizedString.FromId("Menu.Tools.Hashes");
        public static LocalizedString MenuToolsRawImageViewer => LocalizedString.FromId("Menu.Tools.RawImageViewer");

        // Ciphers Menu
        public static LocalizedString MenuCiphersEncrypt => LocalizedString.FromId("Menu.Ciphers.Encrypt");
        public static LocalizedString MenuCiphersDecrypt => LocalizedString.FromId("Menu.Ciphers.Decrypt");

        // Compressions Menu
        public static LocalizedString MenuCompressionsDecompress => LocalizedString.FromId("Menu.Compressions.Decompress");
        public static LocalizedString MenuCompressionsCompress => LocalizedString.FromId("Menu.Compressions.Compress");

        // Settings Menu
        public static LocalizedString MenuSettingsIncludeDevBuilds => LocalizedString.FromId("Menu.Settings.IncludeDevBuilds");
        public static LocalizedString MenuSettingsChangeLanguage => LocalizedString.FromId("Menu.Settings.ChangeLanguage");
        public static LocalizedString MenuSettingsChangeTheme => LocalizedString.FromId("Menu.Settings.ChangeTheme");

        // Theme Menu
        public static LocalizedString MenuSettingsChangeThemeDark => LocalizedString.FromId("Menu.Settings.ChangeTheme.Dark");
        public static LocalizedString MenuSettingsChangeThemeLight => LocalizedString.FromId("Menu.Settings.ChangeTheme.Light");

        // Plugins Dialog
        public static LocalizedString MenuPluginsTitle => LocalizedString.FromId("Menu.Plugins.Title");
        public static LocalizedString MenuPluginsName => LocalizedString.FromId("Menu.Plugins.Name");
        public static LocalizedString MenuPluginsPublisher => LocalizedString.FromId("Menu.Plugins.Publisher");
        public static LocalizedString MenuPluginsDeveloper => LocalizedString.FromId("Menu.Plugins.Developer");
        public static LocalizedString MenuPluginsAuthors => LocalizedString.FromId("Menu.Plugins.Authors");
        public static LocalizedString MenuPluginsPlatforms => LocalizedString.FromId("Menu.Plugins.Platforms");
        public static LocalizedString MenuPluginsType(PluginType type) => type switch
        {
            PluginType.Archive => LocalizedString.FromId("Menu.Plugins.Archive"),
            PluginType.Image => LocalizedString.FromId("Menu.Plugins.Image"),
            PluginType.Font => LocalizedString.FromId("Menu.Plugins.Font"),
            _ => string.Empty
        };

        // About Dialog
        public static LocalizedString MenuAboutTitle => LocalizedString.FromId("Menu.About.Title");
        public static LocalizedString MenuAboutVersion(string version) => LocalizedString.FromId("Menu.About.Version", () => version);
        public static LocalizedString MenuAboutDescription => LocalizedString.FromId("Menu.About.Description");

        // Update Available Dialog
        public static LocalizedString DialogUpdateAvailableCaption => LocalizedString.FromId("Dialog.UpdateAvailable.Text");
        public static LocalizedString DialogUpdateAvailableText(string version, string build, string remoteVersion, string remoteBuild)
            => LocalizedString.FromId("Dialog.UpdateAvailable.Text", () => version, () => build, () => remoteVersion, () => remoteBuild);

        // Exception Dialog
        public static LocalizedString DialogExceptionCatchedCaption => LocalizedString.FromId("Dialog.ExceptionCatched.Caption");

        // Plugins Not Available Dialog
        public static LocalizedString DialogPluginsNotAvailableCaption => LocalizedString.FromId("Dialog.PluginsNotAvailable.Caption");
        public static LocalizedString DialogPluginsNotAvailableText => LocalizedString.FromId("Dialog.PluginsNotAvailable.Text");

        // Unsaved Changes Dialog
        public static LocalizedString DialogUnsavedChangesCaption => LocalizedString.FromId("Dialog.UnsavedChanges.Caption");
        public static LocalizedString DialogUnsavedChangesTextGeneric => LocalizedString.FromId("Dialog.UnsavedChanges.Text.Generic");
        public static LocalizedString DialogUnsavedChangesTextSpecific(UPath path) => LocalizedString.FromId("Dialog.UnsavedChanges.Text.Specific", () => path);

        // Dependant Files Dialog
        public static LocalizedString DialogDependantFilesCaption => LocalizedString.FromId("Dialog.DependantFiles.Caption");
        public static LocalizedString DialogDependantFilesText => LocalizedString.FromId("Dialog.DependantFiles.Text");

        // Status
        public static LocalizedString StatusPluginSelectNone => LocalizedString.FromId("Status.Plugin.Select.None");
        public static LocalizedString StatusPluginSelectUnknown(IFilePluginState state)
            => LocalizedString.FromId("Status.Plugin.Select.Unknown", () => state.GetType().Name);
        public static LocalizedString StatusPluginLoadNone => LocalizedString.FromId("Status.Plugin.Load.None");
        public static LocalizedString StatusPluginLoadNoArchive => LocalizedString.FromId("Status.Plugin.Load.NoArchive");
        public static LocalizedString StatusPluginStateInitError => LocalizedString.FromId("Status.Plugin.State.Init.Error");
        public static LocalizedString StatusPluginStateLoadNone => LocalizedString.FromId("Status.Plugin.State.Load.None");
        public static LocalizedString StatusPluginStateLoadError => LocalizedString.FromId("Status.Plugin.State.Load.Error");
        public static LocalizedString StatusFileSelectNone => LocalizedString.FromId("Status.File.Select.None");
        public static LocalizedString StatusFileSelectInvalid => LocalizedString.FromId("Status.File.Select.Invalid");
        public static LocalizedString StatusFileLoadStart(UPath path) => LocalizedString.FromId("Status.File.Load.Start", () => path);
        public static LocalizedString StatusFileLoadCancel => LocalizedString.FromId("Status.File.Load.Cancel");
        public static LocalizedString StatusFileLoadOpening(UPath path) => LocalizedString.FromId("Status.File.Load.Opening", () => path);
        public static LocalizedString StatusFileLoadSaving(UPath path) => LocalizedString.FromId("Status.File.Load.Saving", () => path);
        public static LocalizedString StatusFileLoadSuccess => LocalizedString.FromId("Status.File.Load.Success");
        public static LocalizedString StatusFileLoadError => LocalizedString.FromId("Status.File.Load.Error");
        public static LocalizedString StatusFileLoadErrorPlugin(Guid id) => LocalizedString.FromId("Status.File.Load.Error.Plugin", () => id);
        public static LocalizedString StatusFileSaveStart(UPath path) => LocalizedString.FromId("Status.File.Save.Start", () => path);
        public static LocalizedString StatusFileSaveClosed => LocalizedString.FromId("Status.File.Save.Closed");
        public static LocalizedString StatusFileSaveSaving(UPath path) => LocalizedString.FromId("Status.File.Save.Saving", () => path);
        public static LocalizedString StatusFileSaveClosing(UPath path) => LocalizedString.FromId("Status.File.Save.Closing", () => path);
        public static LocalizedString StatusFileSaveNotLoaded => LocalizedString.FromId("Status.File.Save.NotLoaded");
        public static LocalizedString StatusFileSaveNoChanges => LocalizedString.FromId("Status.File.Save.NoChanges");
        public static LocalizedString StatusFileSaveNotSupported => LocalizedString.FromId("Status.File.Save.NotSupported");
        public static LocalizedString StatusFileSaveStateError => LocalizedString.FromId("Status.File.Save.State.Error");
        public static LocalizedString StatusFileSaveStateReloadError => LocalizedString.FromId("Status.File.Save.State.Reload.Error");
        public static LocalizedString StatusFileSaveReplaceError => LocalizedString.FromId("Status.File.Save.Replace.Error");
        public static LocalizedString StatusFileSaveCopyError => LocalizedString.FromId("Status.File.Save.Copy.Error");
        public static LocalizedString StatusFileSaveDestinationNotExist => LocalizedString.FromId("Status.File.Save.DestinationNotExist");
        public static LocalizedString StatusFileSaveSuccess => LocalizedString.FromId("Status.File.Save.Success");
        public static LocalizedString StatusFileCloseStart(UPath path) => LocalizedString.FromId("Status.File.Close.Start", () => path);
        public static LocalizedString StatusFileCloseCancel => LocalizedString.FromId("Status.File.Close.Cancel");
        public static LocalizedString StatusFileCloseSaving(UPath path) => LocalizedString.FromId("Status.File.Close.Saving", () => path);
        public static LocalizedString StatusFileCloseClosing(UPath path) => LocalizedString.FromId("Status.File.Close.Closing", () => path);
        public static LocalizedString StatusFileCloseNotLoaded => LocalizedString.FromId("Status.File.Close.NotLoaded");
        public static LocalizedString StatusFileCloseSuccess => LocalizedString.FromId("Status.File.Close.Success");
        public static LocalizedString StatusOperationRunning => LocalizedString.FromId("Status.Operation.Running");

        // Errors
        public static LocalizedString ErrorUnsupportedOperatingSystem(string os)
            => LocalizedString.FromId("Error.Unsupported.OperatingSystem", () => os);

        // File Filters
        public static LocalizedString FilterAll => LocalizedString.FromId("Filter.All");
        public static LocalizedString FilterPng => LocalizedString.FromId("Filter.Png");

        // Archive Form

        // File Operations
        public static LocalizedString ArchiveFileOpen => LocalizedString.FromId("Archive.File.Open");
        public static LocalizedString ArchiveFileOpenWith => LocalizedString.FromId("Archive.File.OpenWith");
        public static LocalizedString ArchiveFileExtract => LocalizedString.FromId("Archive.File.Extract");
        public static LocalizedString ArchiveFileReplace => LocalizedString.FromId("Archive.File.Replace");
        public static LocalizedString ArchiveFileRename => LocalizedString.FromId("Archive.File.Rename");
        public static LocalizedString ArchiveFileDelete => LocalizedString.FromId("Archive.File.Delete");

        // Folder Operations
        public static LocalizedString ArchiveDirectoryExtract => LocalizedString.FromId("Archive.Directory.Extract");
        public static LocalizedString ArchiveDirectoryReplace => LocalizedString.FromId("Archive.Directory.Replace");
        public static LocalizedString ArchiveDirectoryRename => LocalizedString.FromId("Archive.Directory.Rename");
        public static LocalizedString ArchiveDirectoryDelete => LocalizedString.FromId("Archive.Directory.Delete");
        public static LocalizedString ArchiveDirectoryAdd => LocalizedString.FromId("Archive.Directory.Add");

        // Archive Status
        public static LocalizedString ArchiveStatusExtractCancel => LocalizedString.FromId("Archive.Status.Extract.Cancel");
        public static LocalizedString ArchiveStatusReplaceCancel => LocalizedString.FromId("Archive.Status.Replace.Cancel");
        public static LocalizedString ArchiveStatusRenameCancel => LocalizedString.FromId("Archive.Status.Rename.Cancel");
        public static LocalizedString ArchiveStatusDeleteCancel => LocalizedString.FromId("Archive.Status.Delete.Cancel");
        public static LocalizedString ArchiveStatusAddCancel => LocalizedString.FromId("Archive.Status.Add.Cancel");

        public static LocalizedString ArchiveStatusExtractSuccess => LocalizedString.FromId("Archive.Status.Extract.Success");
        public static LocalizedString ArchiveStatusReplaceSuccess => LocalizedString.FromId("Archive.Status.Replace.Success");
        public static LocalizedString ArchiveStatusRenameSuccess => LocalizedString.FromId("Archive.Status.Rename.Success");
        public static LocalizedString ArchiveStatusDeleteSuccess => LocalizedString.FromId("Archive.Status.Delete.Success");
        public static LocalizedString ArchiveStatusAddSuccess => LocalizedString.FromId("Archive.Status.Add.Success");

        public static LocalizedString ArchiveStatusRenameErrorNoName => LocalizedString.FromId("Archive.Status.Rename.Error.NoName");
        public static LocalizedString ArchiveStatusAddError => LocalizedString.FromId("Archive.Status.Add.Error");

        public static LocalizedString ArchiveStatusSelectNone => LocalizedString.FromId("Archive.Status.Select.None");
        public static LocalizedString ArchiveStatusExtractNone => LocalizedString.FromId("Archive.Status.Extract.None");
        public static LocalizedString ArchiveStatusReplaceNone => LocalizedString.FromId("Archive.Status.Replace.None");
        public static LocalizedString ArchiveStatusRenameNone => LocalizedString.FromId("Archive.Status.Rename.None");
        public static LocalizedString ArchiveStatusDeleteNone => LocalizedString.FromId("Archive.Status.Delete.None");
        public static LocalizedString ArchiveStatusAddNone => LocalizedString.FromId("Archive.Status.Add.None");

        // Archive Progress
        public static LocalizedString ArchiveProgressExtract => LocalizedString.FromId("Archive.Progress.Extract");
        public static LocalizedString ArchiveProgressReplace => LocalizedString.FromId("Archive.Progress.Replace");
        public static LocalizedString ArchiveProgressRename => LocalizedString.FromId("Archive.Progress.Rename");
        public static LocalizedString ArchiveProgressDelete => LocalizedString.FromId("Archive.Progress.Delete");
        public static LocalizedString ArchiveProgressAdd => LocalizedString.FromId("Archive.Progress.Add");

        // Archive Rename Dialog
        public static LocalizedString ArchiveDialogRenameFileCaption => LocalizedString.FromId("Archive.Dialog.Rename.File.Caption");
        public static LocalizedString ArchiveDialogRenameDirectoryCaption => LocalizedString.FromId("Archive.Dialog.Rename.Directory.Caption");
        public static LocalizedString ArchiveDialogRenameText(string name) => LocalizedString.FromId("Archive.Dialog.Rename.Text", () => name);

        // Archive File Headers
        public static LocalizedString ArchiveTableFilesName => LocalizedString.FromId("Archive.Table.Files.Name");
        public static LocalizedString ArchiveTableFilesSize => LocalizedString.FromId("Archive.Table.Files.Size");

        // Archive Search Bar
        public static LocalizedString ArchiveSearchPlaceholder => LocalizedString.FromId("Archive.Search.Placeholder");
        public static LocalizedString ArchiveSearchClear => LocalizedString.FromId("Archive.Search.Clear");

        // Misc
        public static LocalizedString ArchiveFileCount(int fileCount) => LocalizedString.FromId("Archive.FileCount", () => fileCount);
        public static LocalizedString ArchiveCancelOperation => LocalizedString.FromId("Archive.CancelOperation");

        // Image Form

        // Menu
        public static LocalizedString ImageMenuExport => LocalizedString.FromId("Image.Menu.Export");
        public static LocalizedString ImageMenuImport => LocalizedString.FromId("Image.Menu.Import");
        public static LocalizedString ImageMenuExportBatch => LocalizedString.FromId("Image.Menu.Export.Batch");
        public static LocalizedString ImageMenuImportBatch => LocalizedString.FromId("Image.Menu.Import.Batch");
        public static LocalizedString ImageMenuExportPng => LocalizedString.FromId("Image.Menu.Export.Png");
        public static LocalizedString ImageMenuImportPng => LocalizedString.FromId("Image.Menu.Import.Png");

        // Labels
        public static LocalizedString ImageLabelWidth => LocalizedString.FromId("Image.Label.Width");
        public static LocalizedString ImageLabelHeight => LocalizedString.FromId("Image.Label.Height");
        public static LocalizedString ImageLabelFormat => LocalizedString.FromId("Image.Label.Format");
        public static LocalizedString ImageLabelPalette => LocalizedString.FromId("Image.Label.Palette");

        // Status
        public static LocalizedString ImageStatusExportStart(string imgName) => LocalizedString.FromId("Image.Status.Export.Start", () => imgName);
        public static LocalizedString ImageStatusExportCancel => LocalizedString.FromId("Image.Status.Export.Cancel");
        public static LocalizedString ImageStatusExportSuccess => LocalizedString.FromId("Image.Status.Export.Success");
        public static LocalizedString ImageStatusExportFailure => LocalizedString.FromId("Image.Status.Export.Failure");
        public static LocalizedString ImageStatusImportStart(string imgName) => LocalizedString.FromId("Image.Status.Import.Start", () => imgName);
        public static LocalizedString ImageStatusImportCancel => LocalizedString.FromId("Image.Status.Import.Cancel");
        public static LocalizedString ImageStatusImportSuccess => LocalizedString.FromId("Image.Status.Import.Success");
        public static LocalizedString ImageStatusImportFailure => LocalizedString.FromId("Image.Status.Import.Failure");

        // Image Progress
        public static LocalizedString ImageProgressDecode => LocalizedString.FromId("Image.Progress.Decode");

        // Font Form

        // Labels
        public static LocalizedString FontLabelBaseLine => LocalizedString.FromId("Font.Label.BaseLine");
        public static LocalizedString FontLabelDescentLine => LocalizedString.FromId("Font.Label.DescentLine");
        public static LocalizedString FontSearchPlaceholder => LocalizedString.FromId("Font.Search.Placeholder");
        public static LocalizedString FontPreviewPlaceholder=> LocalizedString.FromId("Font.Preview.Placeholder");

        // Generate
        public static LocalizedString FontGenerateCaption => LocalizedString.FromId("Font.Generate.Caption");

        // Dialogs

        // Dialog Manager
        public static LocalizedString DialogManagerButtonOk => LocalizedString.FromId("Dialog.Manager.Button.Ok");

        // Choose Plugin Dialog
        public static LocalizedString DialogChoosePluginCaption => LocalizedString.FromId("Dialog.ChoosePlugin.Caption");

        public static LocalizedString DialogChoosePluginHeaderGeneric => LocalizedString.FromId("Dialog.ChoosePlugin.Header.Generic");
        public static LocalizedString DialogChoosePluginHeaderIdentificationNone => LocalizedString.FromId("Dialog.ChoosePlugin.Header.Identification.None");
        public static LocalizedString DialogChoosePluginHeaderIdentificationMultiple => LocalizedString.FromId("Dialog.ChoosePlugin.Header.Identification.Multiple");
        public static LocalizedString DialogChoosePluginHeaderIdentificationNote => LocalizedString.FromId("Dialog.ChoosePlugin.Header.Identification.Note");

        public static LocalizedString DialogChoosePluginPluginsTableName => LocalizedString.FromId("Dialog.ChoosePlugin.Plugins.Table.Name");
        public static LocalizedString DialogChoosePluginPluginsTableType => LocalizedString.FromId("Dialog.ChoosePlugin.Plugins.Table.Type");
        public static LocalizedString DialogChoosePluginPluginsTableDescription => LocalizedString.FromId("Dialog.ChoosePlugin.Plugins.Table.Description");
        public static LocalizedString DialogChoosePluginPluginsTableId => LocalizedString.FromId("Dialog.ChoosePlugin.Plugins.Table.ID");

        public static LocalizedString DialogChoosePluginContinue => LocalizedString.FromId("Dialog.ChoosePlugin.Continue");
        public static LocalizedString DialogChoosePluginViewRaw => LocalizedString.FromId("Dialog.ChoosePlugin.ViewRaw");
        public static LocalizedString DialogChoosePluginCancel => LocalizedString.FromId("Dialog.ChoosePlugin.Cancel");
        public static LocalizedString DialogChoosePluginShowAll => LocalizedString.FromId("Dialog.ChoosePlugin.ShowAll");

        // Font Generation Dialog
        public static LocalizedString DialogGenerateFontCaption => LocalizedString.FromId("Dialog.GenerateFont.Caption");

        public static LocalizedString DialogGenerateFontFamily => LocalizedString.FromId("Dialog.GenerateFont.Family");
        public static LocalizedString DialogGenerateFontStyle => LocalizedString.FromId("Dialog.GenerateFont.Style");
        public static LocalizedString DialogGenerateFontSize => LocalizedString.FromId("Dialog.GenerateFont.Size");
        public static LocalizedString DialogGenerateBaseline => LocalizedString.FromId("Dialog.GenerateFont.Baseline");
        public static LocalizedString DialogGenerateGlyphHeight => LocalizedString.FromId("Dialog.GenerateFont.GlyphHeight");
        public static LocalizedString DialogGenerateSpaceWidth => LocalizedString.FromId("Dialog.GenerateFont.SpaceWidth");
        public static LocalizedString DialogGenerateCharacters => LocalizedString.FromId("Dialog.GenerateFont.Characters");

        public static LocalizedString DialogGenerateFontStyleBold => LocalizedString.FromId("Dialog.GenerateFont.Style.Bold");
        public static LocalizedString DialogGenerateFontStyleItalic => LocalizedString.FromId("Dialog.GenerateFont.Style.Italic");

        public static LocalizedString DialogGenerateFontLoad => LocalizedString.FromId("Dialog.GenerateFont.Load");
        public static LocalizedString DialogGenerateFontLoadCaption => LocalizedString.FromId("Dialog.GenerateFont.Load.Caption");

        public static LocalizedString DialogGenerateFontSave => LocalizedString.FromId("Dialog.GenerateFont.Save");
        public static LocalizedString DialogGenerateFontSaveCaption => LocalizedString.FromId("Dialog.GenerateFont.Save.Caption");

        public static LocalizedString DialogGenerateFontProfile => LocalizedString.FromId("Dialog.GenerateFont.Profile");

        public static LocalizedString DialogGenerateFontGenerate => LocalizedString.FromId("Dialog.GenerateFont.Generate");

        public static LocalizedString DialogGenerateFontPaddingLeft => LocalizedString.FromId("Dialog.GenerateFont.Padding.Left");
        public static LocalizedString DialogGenerateFontPaddingRight => LocalizedString.FromId("Dialog.GenerateFont.Padding.Right");
    }
}
