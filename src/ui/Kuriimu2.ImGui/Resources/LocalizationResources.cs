using System;
using ImGui.Forms.Localization;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.FileSystem;

namespace Kuriimu2.ImGui.Resources
{
    static class LocalizationResources
    {
        private static readonly Lazy<Localizer> Lazy = new(new Localizer());
        public static ILocalizer Instance => Lazy.Value;

        // Kuriimu2

        // Menus
        public static LocalizedString MenuFile => new("Menu.File");
        public static LocalizedString MenuTools => new("Menu.Tools");
        public static LocalizedString MenuCiphers => new("Menu.Ciphers");
        public static LocalizedString MenuCompressions => new("Menu.Compressions");
        public static LocalizedString MenuSettings => new("Menu.Settings");
        public static LocalizedString MenuHelp => new("Menu.Help");

        // File Menu
        public static LocalizedString MenuFileOpen => new("Menu.File.Open");
        public static LocalizedString MenuFileOpenWith => new("Menu.File.OpenWith");
        public static LocalizedString MenuFileSave => new("Menu.File.Save");
        public static LocalizedString MenuFileSaveAs => new("Menu.File.SaveAs");
        public static LocalizedString MenuFileSaveAll => new("Menu.File.SaveAll");
        public static LocalizedString MenuFileClose => new("Menu.File.Close");

        // Tools Menu
        public static LocalizedString MenuToolsTextSequenceSearcher => new("Menu.Tools.TextSequenceSearcher");
        public static LocalizedString MenuToolsBatchExtractor => new("Menu.Tools.BatchExtractor");
        public static LocalizedString MenuToolsBatchInjector => new("Menu.Tools.BatchInjector");
        public static LocalizedString MenuToolsHashes => new("Menu.Tools.Hashes");
        public static LocalizedString MenuToolsRawImageViewer => new("Menu.Tools.RawImageViewer");

        // Ciphers Menu
        public static LocalizedString MenuCiphersEncrypt => new("Menu.Ciphers.Encrypt");
        public static LocalizedString MenuCiphersDecrypt => new("Menu.Ciphers.Decrypt");

        // Compressions Menu
        public static LocalizedString MenuCompressionsDecompress => new("Menu.Compressions.Decompress");
        public static LocalizedString MenuCompressionsCompress => new("Menu.Compressions.Compress");

        // Settings Menu
        public static LocalizedString MenuSettingsIncludeDevBuilds => new("Menu.Settings.IncludeDevBuilds");
        public static LocalizedString MenuSettingsChangeLanguage => new("Menu.Settings.ChangeLanguage");
        public static LocalizedString MenuSettingsChangeTheme => new("Menu.Settings.ChangeTheme");

        // Theme Menu
        public static LocalizedString MenuSettingsChangeThemeDark => new("Menu.Settings.ChangeTheme.Dark");
        public static LocalizedString MenuSettingsChangeThemeLight => new("Menu.Settings.ChangeTheme.Light");

        // About Dialog
        public static LocalizedString MenuAboutTitle => new("Menu.About.Title");
        public static LocalizedString MenuAboutVersion(string version) => new("Menu.About.Version", () => version);
        public static LocalizedString MenuAboutDescription => new("Menu.About.Description");

        // Update Available Dialog
        public static LocalizedString DialogUpdateAvailableCaption => new("Dialog.UpdateAvailable.Text");
        public static LocalizedString DialogUpdateAvailableText(string version, string build, string remoteVersion, string remoteBuild)
            => new("Dialog.UpdateAvailable.Text", () => version, () => build, () => remoteVersion, () => remoteBuild);

        // Exception Dialog
        public static LocalizedString DialogExceptionCatchedCaption => new("Dialog.ExceptionCatched.Caption");

        // Plugins Not Available Dialog
        public static LocalizedString DialogPluginsNotAvailableCaption => new("Dialog.PluginsNotAvailable.Caption");
        public static LocalizedString DialogPluginsNotAvailableText => new("Dialog.PluginsNotAvailable.Text");

        // Unsaved Changes Dialog
        public static LocalizedString DialogUnsavedChangesCaption => new("Dialog.UnsavedChanges.Caption");
        public static LocalizedString DialogUnsavedChangesTextGeneric => new("Dialog.UnsavedChanges.Text.Generic");
        public static LocalizedString DialogUnsavedChangesTextSpecific(UPath path) => new("Dialog.UnsavedChanges.Text.Specific", () => path);

        // Dependant Files Dialog
        public static LocalizedString DialogDependantFilesCaption => new("Dialog.DependantFiles.Caption");
        public static LocalizedString DialogDependantFilesText => new("Dialog.DependantFiles.Text");

        // Status
        public static LocalizedString StatusPluginSelectNone => new("Status.Plugin.Select.None");
        public static LocalizedString StatusPluginSelectUnknown(IPluginState state) => new("Status.Plugin.Select.Unknown", () => state.GetType().Name);
        public static LocalizedString StatusPluginLoadNone => new("Status.Plugin.Load.None");
        public static LocalizedString StatusPluginLoadNoArchive => new("Status.Plugin.Load.NoArchive");
        public static LocalizedString StatusPluginStateInitError => new("Status.Plugin.State.Init.Error");
        public static LocalizedString StatusPluginStateLoadNone => new("Status.Plugin.State.Load.None");
        public static LocalizedString StatusPluginStateLoadError => new("Status.Plugin.State.Load.Error");
        public static LocalizedString StatusFileSelectNone => new("Status.File.Select.None");
        public static LocalizedString StatusFileSelectInvalid => new("Status.File.Select.Invalid");
        public static LocalizedString StatusFileLoadStart(UPath path) => new("Status.File.Load.Start", () => path);
        public static LocalizedString StatusFileLoadCancel => new("Status.File.Load.Cancel");
        public static LocalizedString StatusFileLoadOpening(UPath path) => new("Status.File.Load.Opening", () => path);
        public static LocalizedString StatusFileLoadSaving(UPath path) => new("Status.File.Load.Saving", () => path);
        public static LocalizedString StatusFileLoadSuccess => new("Status.File.Load.Success");
        public static LocalizedString StatusFileLoadError => new("Status.File.Load.Error");
        public static LocalizedString StatusFileLoadErrorPlugin(Guid id) => new("Status.File.Load.Error.Plugin", () => id);
        public static LocalizedString StatusFileSaveStart(UPath path) => new("Status.File.Save.Start", () => path);
        public static LocalizedString StatusFileSaveClosed => new("Status.File.Save.Closed");
        public static LocalizedString StatusFileSaveSaving(UPath path) => new("Status.File.Save.Saving", () => path);
        public static LocalizedString StatusFileSaveClosing(UPath path) => new("Status.File.Save.Closing", () => path);
        public static LocalizedString StatusFileSaveNotLoaded => new("Status.File.Save.NotLoaded");
        public static LocalizedString StatusFileSaveNoChanges => new("Status.File.Save.NoChanges");
        public static LocalizedString StatusFileSaveStateError => new("Status.File.Save.State.Error");
        public static LocalizedString StatusFileSaveStateReloadError => new("Status.File.Save.State.Reload.Error");
        public static LocalizedString StatusFileSaveReplaceError => new("Status.File.Save.Replace.Error");
        public static LocalizedString StatusFileSaveCopyError => new("Status.File.Save.Copy.Error");
        public static LocalizedString StatusFileSaveDestinationNotExist => new("Status.File.Save.DestinationNotExist");
        public static LocalizedString StatusFileSaveSuccess => new("Status.File.Save.Success");
        public static LocalizedString StatusFileSaveError => new("Status.File.Save.Error");
        public static LocalizedString StatusFileCloseStart(UPath path) => new("Status.File.Close.Start", () => path);
        public static LocalizedString StatusFileCloseCancel => new("Status.File.Close.Cancel");
        public static LocalizedString StatusFileCloseSaving(UPath path) => new("Status.File.Close.Saving", () => path);
        public static LocalizedString StatusFileCloseClosing(UPath path) => new("Status.File.Close.Closing", () => path);
        public static LocalizedString StatusFileCloseNotLoaded => new("Status.File.Close.NotLoaded");
        public static LocalizedString StatusFileCloseSuccess => new("Status.File.Close.Success");
        public static LocalizedString StatusOperationRunning => new("Status.Operation.Running");

        // Errors
        public static LocalizedString ErrorUnsupportedOperatingSystem(string os) => new("Error.Unsupported.OperatingSystem", () => os);

        // File Filters
        public static LocalizedString FilterAll => new("Filter.All");
        public static LocalizedString FilterPng => new("Filter.Png");

        // Archive Form

        // File Operations
        public static LocalizedString ArchiveFileExtract => new("Archive.File.Extract");
        public static LocalizedString ArchiveFileReplace => new("Archive.File.Replace");
        public static LocalizedString ArchiveFileRename => new("Archive.File.Rename");
        public static LocalizedString ArchiveFileDelete => new("Archive.File.Delete");

        // Folder Operations
        public static LocalizedString ArchiveDirectoryExtract => new("Archive.Directory.Extract");
        public static LocalizedString ArchiveDirectoryReplace => new("Archive.Directory.Replace");
        public static LocalizedString ArchiveDirectoryRename => new("Archive.Directory.Rename");
        public static LocalizedString ArchiveDirectoryDelete => new("Archive.Directory.Delete");
        public static LocalizedString ArchiveDirectoryAdd => new("Archive.Directory.Add");

        // Archive Status
        public static LocalizedString ArchiveStatusExtractCancel => new("Archive.Status.Extract.Cancel");
        public static LocalizedString ArchiveStatusReplaceCancel => new("Archive.Status.Replace.Cancel");
        public static LocalizedString ArchiveStatusRenameCancel => new("Archive.Status.Rename.Cancel");
        public static LocalizedString ArchiveStatusDeleteCancel => new("Archive.Status.Delete.Cancel");
        public static LocalizedString ArchiveStatusAddCancel => new("Archive.Status.Add.Cancel");

        public static LocalizedString ArchiveStatusExtractSuccess => new("Archive.Status.Extract.Success");
        public static LocalizedString ArchiveStatusReplaceSuccess => new("Archive.Status.Replace.Success");
        public static LocalizedString ArchiveStatusRenameSuccess => new("Archive.Status.Rename.Success");
        public static LocalizedString ArchiveStatusDeleteSuccess => new("Archive.Status.Delete.Success");
        public static LocalizedString ArchiveStatusAddSuccess => new("Archive.Status.Add.Success");

        public static LocalizedString ArchiveStatusRenameErrorNoName => new("Archive.Status.Rename.Error.NoName");
        public static LocalizedString ArchiveStatusAddError => new("Archive.Status.Add.Error");

        public static LocalizedString ArchiveStatusSelectNone => new("Archive.Status.Select.None");
        public static LocalizedString ArchiveStatusExtractNone => new("Archive.Status.Extract.None");
        public static LocalizedString ArchiveStatusReplaceNone => new("Archive.Status.Replace.None");
        public static LocalizedString ArchiveStatusRenameNone => new("Archive.Status.Rename.None");
        public static LocalizedString ArchiveStatusDeleteNone => new("Archive.Status.Delete.None");
        public static LocalizedString ArchiveStatusAddNone => new("Archive.Status.Add.None");

        // Archive Progress
        public static LocalizedString ArchiveProgressExtract => new("Archive.Progress.Extract");
        public static LocalizedString ArchiveProgressReplace => new("Archive.Progress.Replace");
        public static LocalizedString ArchiveProgressRename => new("Archive.Progress.Rename");
        public static LocalizedString ArchiveProgressDelete => new("Archive.Progress.Delete");
        public static LocalizedString ArchiveProgressAdd => new("Archive.Progress.Add");

        // Archive Rename Dialog
        public static LocalizedString ArchiveDialogRenameFileCaption => new("Archive.Dialog.Rename.File.Caption");
        public static LocalizedString ArchiveDialogRenameDirectoryCaption => new("Archive.Dialog.Rename.Directory.Caption");
        public static LocalizedString ArchiveDialogRenameText(string name) => new("Archive.Dialog.Rename.Text", () => name);

        // Archive File Headers
        public static LocalizedString ArchiveTableFilesName => new("Archive.Table.Files.Name");
        public static LocalizedString ArchiveTableFilesSize => new("Archive.Table.Files.Size");

        // Archive Search Bar
        public static LocalizedString ArchiveSearchPlaceholder => new("Archive.Search.Placeholder");
        public static LocalizedString ArchiveSearchClear => new("Archive.Search.Clear");

        // Misc
        public static LocalizedString ArchiveFileCount(int fileCount) => new("Archive.FileCount", () => fileCount);
        public static LocalizedString ArchiveCancelOperation => new("Archive.CancelOperation");

        // Image Form

        // Menu
        public static LocalizedString ImageMenuExport => new("Image.Menu.Export");
        public static LocalizedString ImageMenuImport => new("Image.Menu.Import");
        public static LocalizedString ImageMenuExportPng => new("Image.Menu.Export.Png");
        public static LocalizedString ImageMenuImportPng => new("Image.Menu.Import.Png");

        // Labels
        public static LocalizedString ImageLabelWidth => new("Image.Label.Width");
        public static LocalizedString ImageLabelHeight => new("Image.Label.Height");
        public static LocalizedString ImageLabelFormat => new("Image.Label.Format");
        public static LocalizedString ImageLabelPalette => new("Image.Label.Palette");

        // Image Status
        public static LocalizedString ImageStatusImportSuccess => new("Image.Status.Import.Success");

        // Image Progress
        public static LocalizedString ImageProgressDecode => new("Image.Progress.Decode");

        // Dialogs

        // Dialog Manager
        public static LocalizedString DialogManagerButtonOk => new("Dialog.Manager.Button.Ok");

        // Choose Plugin Dialog
        public static LocalizedString DialogChoosePluginCaption => new("Dialog.ChoosePlugin.Caption");

        public static LocalizedString DialogChoosePluginHeaderGeneric => new("Dialog.ChoosePlugin.Header.Generic");
        public static LocalizedString DialogChoosePluginHeaderIdentificationNone => new("Dialog.ChoosePlugin.Header.Identification.None");
        public static LocalizedString DialogChoosePluginHeaderIdentificationMultiple => new("Dialog.ChoosePlugin.Header.Identification.Multiple");
        public static LocalizedString DialogChoosePluginHeaderIdentificationNote => new("Dialog.ChoosePlugin.Header.Identification.Note");

        public static LocalizedString DialogChoosePluginPluginsTableName => new("Dialog.ChoosePlugin.Plugins.Table.Name");
        public static LocalizedString DialogChoosePluginPluginsTableType => new("Dialog.ChoosePlugin.Plugins.Table.Type");
        public static LocalizedString DialogChoosePluginPluginsTableDescription => new("Dialog.ChoosePlugin.Plugins.Table.Description");
        public static LocalizedString DialogChoosePluginPluginsTableId => new("Dialog.ChoosePlugin.Plugins.Table.ID");

        public static LocalizedString DialogChoosePluginContinue => new("Dialog.ChoosePlugin.Continue");
        public static LocalizedString DialogChoosePluginViewRaw => new("Dialog.ChoosePlugin.ViewRaw");
        public static LocalizedString DialogChoosePluginCancel => new("Dialog.ChoosePlugin.Cancel");
        public static LocalizedString DialogChoosePluginShowAll => new("Dialog.ChoosePlugin.ShowAll");
    }
}
