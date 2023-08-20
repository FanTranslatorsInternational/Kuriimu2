using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ImGui.Forms.Localization;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.FileSystem;
using Kontract.Models.Managers.Dialogs;
using Newtonsoft.Json;

namespace Kuriimu2.ImGui.Resources
{
    static class LocalizationResources
    {
        private static readonly Lazy<Localizer> Lazy = new Lazy<Localizer>(new Localizer());
        public static ILocalizer Instance => Lazy.Value;

        #region Support methods

        public static IEnumerable<string> GetLocales()
        {
            return Lazy.Value.GetLocales();
        }

        public static string GetLanguageName(string locale)
        {
            return Lazy.Value.GetLanguageName(locale);
        }

        public static string GetLocaleByName(string name)
        {
            return Lazy.Value.GetLocaleByName(name);
        }

        #endregion

        #region Localization Id's

        // Kuriimu2

        // Menu bar texts
        private const string MenuFileId_ = "Menu.File";
        private const string MenuToolsId_ = "Menu.Tools";
        private const string MenuCiphersId_ = "Menu.Ciphers";
        private const string MenuCompressionsId_ = "Menu.Compressions";
        private const string MenuSettingsId_ = "Menu.Settings";

        // File sub menu
        private const string MenuFileOpenId_ = "Menu.File.Open";
        private const string MenuFileOpenWithId_ = "Menu.File.OpenWith";
        private const string MenuFileSaveId_ = "Menu.File.Save";
        private const string MenuFileSaveAsId_ = "Menu.File.SaveAs";
        private const string MenuFileSaveAllId_ = "Menu.File.SaveAll";
        private const string MenuFileCloseId_ = "Menu.File.Close";

        // Tools sub menu
        private const string MenuToolsTextSequenceSearcherId_ = "Menu.Tools.TextSequenceSearcher";
        private const string MenuToolsBatchExtractorId_ = "Menu.Tools.BatchExtractor";
        private const string MenuToolsBatchInjectorId_ = "Menu.Tools.BatchInjector";
        private const string MenuToolsHashesId_ = "Menu.Tools.Hashes";
        private const string MenuToolsRawImageViewerId_ = "Menu.Tools.RawImageViewer";

        // Ciphers sub menu
        private const string MenuCiphersEncryptId_ = "Menu.Ciphers.Encrypt";
        private const string MenuCiphersDecryptId_ = "Menu.Ciphers.Decrypt";

        // Compressions sub menu
        private const string MenuCompressionsDecompressId_ = "Menu.Compressions.Decompress";
        private const string MenuCompressionsCompressId_ = "Menu.Compressions.Compress";

        // Settings sub menu
        private const string MenuSettingsIncludeDevBuildsId_ = "Menu.Settings.IncludeDevBuilds";
        private const string MenuSettingsChangeLanguageId_ = "Menu.Settings.ChangeLanguage";
        private const string MenuSettingsChangeThemeId_ = "Menu.Settings.ChangeTheme";

        // Change theme sub menu
        private const string MenuSettingsChangeThemeDarkId_ = "Menu.Settings.ChangeTheme.Dark";
        private const string MenuSettingsChangeThemeLightId_ = "Menu.Settings.ChangeTheme.Light";

        // Change language dialog
        private const string DialogChangeLanguageCaptionId_ = "Dialog.ChangeLanguage.Caption";
        private const string DialogChangeLanguageTextId_ = "Dialog.ChangeLanguage.Text";

        // Change theme dialog
        private const string DialogChangeThemeRestartCaptionId_ = "Dialog.ChangeTheme.Restart.Caption";
        private const string DialogChangeThemeRestartTextId_ = "Dialog.ChangeTheme.Restart.Text";

        // Update available dialog
        private const string DialogUpdateAvailableCaptionId_ = "Dialog.UpdateAvailable.Text";
        private const string DialogUpdateAvailableTextId_ = "Dialog.UpdateAvailable.Text";

        // Exception catched dialog
        private const string DialogExceptionCatchedCaptionId_ = "Dialog.ExceptionCatched.Caption";

        // Unhandled exception dialog
        private const string DialogUnhandledExceptionCaptionId_ = "Dialog.UnhandledException.Caption";
        private const string DialogUnhandledExceptionTextCloseId_ = "Dialog.UnhandledException.Text.Close";
        private const string DialogUnhandledExceptionTextNotCloseId_ = "Dialog.UnhandledException.Text.NotClose";

        // Plugins not available dialog
        private const string DialogPluginsNotAvailableCaptionId_ = "Dialog.PluginsNotAvailable.Caption";
        private const string DialogPluginsNotAvailableTextId_ = "Dialog.PluginsNotAvailable.Text";

        // Load error dialog
        private const string DialogLoadErrorCaptionId_ = "Dialog.LoadError.Caption";

        // Unsaved changes dialog
        private const string DialogUnsavedChangesCaptionId_ = "Dialog.UnsavedChanges.Caption";
        private const string DialogUnsavedChangesTextSpecificId_ = "Dialog.UnsavedChanges.Text.Specific";
        private const string DialogUnsavedChangesTextGenericId_ = "Dialog.UnsavedChanges.Text.Generic";

        // Dependant files dialog
        private const string DialogDependantFilesCaptionId_ = "Dialog.DependantFiles.Caption";
        private const string DialogDependantFilesTextId_ = "Dialog.DependantFiles.Text";

        // Save error dialog
        private const string DialogSaveErrorCaptionId_ = "Dialog.SaveError.Caption";

        // Status labels
        private const string StatusPluginSelectNoneId_ = "Status.Plugin.Select.None";
        private const string StatusPluginSelectUnknownId_ = "Status.Plugin.Select.Unknown";
        private const string StatusPluginLoadNoneId_ = "Status.Plugin.Load.None";
        private const string StatusPluginLoadNoArchiveId_ = "Status.Plugin.Load.NoArchive";
        private const string StatusPluginStateInitErrorId_ = "Status.Plugin.State.Init.Error";
        private const string StatusPluginStateLoadNoneId_ = "Status.Plugin.State.Load.None";
        private const string StatusPluginStateLoadErrorId_ = "Status.Plugin.State.Load.Error";
        private const string StatusFileSelectNoneId_ = "Status.File.Select.None";
        private const string StatusFileSelectInvalidId_ = "Status.File.Select.Invalid";
        private const string StatusFileLoadStartId_ = "Status.File.Load.Start";
        private const string StatusFileLoadCancelId_ = "Status.File.Load.Cancel";
        private const string StatusFileLoadOpeningId_ = "Status.File.Load.Opening";
        private const string StatusFileLoadSavingId_ = "Status.File.Load.Saving";
        private const string StatusFileLoadSuccessId_ = "Status.File.Load.Success";
        private const string StatusFileLoadFailureId_ = "Status.File.Load.Error";
        private const string StatusFileLoadFailureWithPluginId_ = "Status.File.Load.FailureWithPlugin";
        private const string StatusFileSaveStartId_ = "Status.File.Save.Start";
        private const string StatusFileSaveClosedId_ = "Status.File.Save.Closed";
        private const string StatusFileSaveSavingId_ = "Status.File.Save.Saving";
        private const string StatusFileSaveClosingId_ = "Status.File.Save.Closing";
        private const string StatusFileSaveNotLoadedId_ = "Status.File.Save.NotLoaded";
        private const string StatusFileSaveNoChangesId_ = "Status.File.Save.NoChanges";
        private const string StatusFileSaveStateErrorId_ = "Status.File.Save.State.Error";
        private const string StatusFileSaveStateReloadErrorId_ = "Status.File.Save.State.Reload.Error";
        private const string StatusFileSaveReplaceErrorId_ = "Status.File.Save.Replace.Error";
        private const string StatusFileSaveCopyErrorId_ = "Status.File.Save.Copy.Error";
        private const string StatusFileSaveDestinationNotExistId_ = "Status.File.Save.DestinationNotExist";
        private const string StatusFileSaveSuccessId_ = "Status.File.Save.Success";
        private const string StatusFileSaveFailureId_ = "Status.File.Save.Error";
        private const string StatusFileCloseStartId_ = "Status.File.Close.Start";
        private const string StatusFileCloseCancelId_ = "Status.File.Close.Cancel";
        private const string StatusFileCloseSavingId_ = "Status.File.Close.Saving";
        private const string StatusFileCloseClosingId_ = "Status.File.Close.Closing";
        private const string StatusFileCloseNotLoadedId_ = "Status.File.Close.NotLoaded";
        private const string StatusFileCloseSuccessId_ = "Status.File.Close.Success";
        private const string StatusOperationRunningId_ = "Status.Operation.Running";

        // Error messages
        private const string ErrorUnsupportedOperatingSystemId_ = "Error.Unsupported.OperatingSystem";

        // File filters
        private const string FilterAllId_ = "Filter.All";
        private const string FilterPngId_ = "Filter.Png";


        // Archive Form

        // File operations
        private const string ArchiveFileExtractId_ = "Archive.File.Extract";
        private const string ArchiveFileReplaceId_ = "Archive.File.Replace";
        private const string ArchiveFileRenameId_ = "Archive.File.Rename";
        private const string ArchiveFileDeleteId_ = "Archive.File.Delete";

        // Folder operations
        private const string ArchiveDirectoryExtractId_ = "Archive.Directory.Extract";
        private const string ArchiveDirectoryReplaceId_ = "Archive.Directory.Replace";
        private const string ArchiveDirectoryRenameId_ = "Archive.Directory.Rename";
        private const string ArchiveDirectoryDeleteId_ = "Archive.Directory.Delete";
        private const string ArchiveDirectoryAddId_ = "Archive.Directory.Add";

        // Status labels
        private const string ArchiveStatusExtractCancelId_ = "Archive.Status.Extract.Cancel";
        private const string ArchiveStatusReplaceCancelId_ = "Archive.Status.Replace.Cancel";
        private const string ArchiveStatusRenameCancelId_ = "Archive.Status.Rename.Cancel";
        private const string ArchiveStatusDeleteCancelId_ = "Archive.Status.Delete.Cancel";
        private const string ArchiveStatusAddCancelId_ = "Archive.Status.Add.Cancel";

        private const string ArchiveStatusExtractSuccessId_ = "Archive.Status.Extract.Success";
        private const string ArchiveStatusReplaceSuccessId_ = "Archive.Status.Replace.Success";
        private const string ArchiveStatusRenameSuccessId_ = "Archive.Status.Rename.Success";
        private const string ArchiveStatusDeleteSuccessId_ = "Archive.Status.Delete.Success";
        private const string ArchiveStatusAddSuccessId_ = "Archive.Status.Add.Success";

        private const string ArchiveStatusRenameErrorNoNameId_ = "Archive.Status.Rename.Error.NoName";
        private const string ArchiveStatusAddErrorId_ = "Archive.Status.Add.Error";

        private const string ArchiveStatusSelectNoneId_ = "Archive.Status.Select.None";
        private const string ArchiveStatusExtractNoneId_ = "Archive.Status.Extract.None";
        private const string ArchiveStatusReplaceNoneId_ = "Archive.Status.Replace.None";
        private const string ArchiveStatusRenameNoneId_ = "Archive.Status.Rename.None";
        private const string ArchiveStatusDeleteNoneId_ = "Archive.Status.Delete.None";
        private const string ArchiveStatusAddNoneId_ = "Archive.Status.Add.None";

        // Progress labels
        private const string ArchiveProgressExtractId_ = "Archive.Progress.Extract";
        private const string ArchiveProgressReplaceId_ = "Archive.Progress.Replace";
        private const string ArchiveProgressRenameId_ = "Archive.Progress.Rename";
        private const string ArchiveProgressDeleteId_ = "Archive.Progress.Delete";
        private const string ArchiveProgressAddId_ = "Archive.Progress.Add";

        // Rename dialog
        private const string ArchiveDialogRenameFileCaptionId_ = "Archive.Dialog.Rename.File.Caption";
        private const string ArchiveDialogRenameDirectoryCaptionId_ = "Archive.Dialog.Rename.Directory.Caption";
        private const string ArchiveDialogRenameTextId_ = "Archive.Dialog.Rename.Text";

        // File headers
        private const string ArchiveTableFilesNameId_ = "Archive.Table.Files.Name";
        private const string ArchiveTableFilesSizeId_ = "Archive.Table.Files.Size";

        // Search bar
        private const string ArchiveSearchPlaceholderId_ = "Archive.Search.Placeholder";
        private const string ArchiveSearchClearId_ = "Archive.Search.Clear";

        // Misc
        private const string ArchiveFileCountId_ = "Archive.FileCount";
        private const string ArchiveCancelOperationId_ = "Archive.CancelOperation";


        // Image Form

        // Menu
        private const string ImageMenuExportId_ = "Image.Menu.Export";
        private const string ImageMenuImportId_ = "Image.Menu.Import";
        private const string ImageMenuExportPngId_ = "Image.Menu.Export.Png";
        private const string ImageMenuImportPngId_ = "Image.Menu.Import.Png";

        // Labels
        private const string ImageLabelWidthId_ = "Image.Label.Width";
        private const string ImageLabelHeightId_ = "Image.Label.Height";
        private const string ImageLabelFormatId_ = "Image.Label.Format";
        private const string ImageLabelPaletteId_ = "Image.Label.Palette";

        // Status
        private const string ImageStatusImportSuccessId_ = "Image.Status.Import.Success";

        // Progress
        private const string ImageProgressDecodeId_ = "Image.Progress.Decode";


        // Dialogs

        // Dialog manager
        private const string DialogManagerButtonOkId_ = "Dialog.Manager.Button.Ok";

        // Choose plugin dialog
        private const string DialogChoosePluginCaptionId_ = "Dialog.ChoosePlugin.Caption";

        private const string DialogChoosePluginHeaderGenericId_ = "Dialog.ChoosePlugin.Header.Generic";
        private const string DialogChoosePluginHeaderIdentificationNoneId_ = "Dialog.ChoosePlugin.Header.Identification.None";
        private const string DialogChoosePluginHeaderIdentificationMultipleId_ = "Dialog.ChoosePlugin.Header.Identification.Multiple";
        private const string DialogChoosePluginHeaderIdentificationNoteId_ = "Dialog.ChoosePlugin.Header.Identification.Note";

        private const string DialogChoosePluginPluginsTableNameId_ = "Dialog.ChoosePlugin.Plugins.Table.Name";
        private const string DialogChoosePluginPluginsTableTypeId_ = "Dialog.ChoosePlugin.Plugins.Table.Type";
        private const string DialogChoosePluginPluginsTableDescriptionId_ = "Dialog.ChoosePlugin.Plugins.Table.Description";
        private const string DialogChoosePluginPluginsTableIdId_ = "Dialog.ChoosePlugin.Plugins.Table.ID";

        private const string DialogChoosePluginContinueId_ = "Dialog.ChoosePlugin.Continue";
        private const string DialogChoosePluginViewRawId_ = "Dialog.ChoosePlugin.ViewRaw";
        private const string DialogChoosePluginCancelId_ = "Dialog.ChoosePlugin.Cancel";
        private const string DialogChoosePluginShowAllId_ = "Dialog.ChoosePlugin.ShowAll";

        #endregion

        #region Resource Instances

        // Kuriimu2

        // Menu bar texts
        public static LocalizedString MenuFile() => new LocalizedString(MenuFileId_);
        public static LocalizedString MenuTools() => new LocalizedString(MenuToolsId_);
        public static LocalizedString MenuCiphers() => new LocalizedString(MenuCiphersId_);
        public static LocalizedString MenuCompressions() => new LocalizedString(MenuCompressionsId_);
        public static LocalizedString MenuSettings() => new LocalizedString(MenuSettingsId_);

        // File sub menu
        public static LocalizedString MenuFileOpen() => new LocalizedString(MenuFileOpenId_);
        public static LocalizedString MenuFileOpenWith() => new LocalizedString(MenuFileOpenWithId_);
        public static LocalizedString MenuFileSaveAll() => new LocalizedString(MenuFileSaveAllId_);

        // Tools sub menu
        public static LocalizedString MenuToolsTextSequenceSearcher() => new LocalizedString(MenuToolsTextSequenceSearcherId_);
        public static LocalizedString MenuToolsBatchExtractor() => new LocalizedString(MenuToolsBatchExtractorId_);
        public static LocalizedString MenuToolsBatchInjector() => new LocalizedString(MenuToolsBatchInjectorId_);
        public static LocalizedString MenuToolsHashes() => new LocalizedString(MenuToolsHashesId_);
        public static LocalizedString MenuToolsRawImageViewer() => new LocalizedString(MenuToolsRawImageViewerId_);

        // Ciphers sub menu
        public static LocalizedString MenuCiphersEncrypt() => new LocalizedString(MenuCiphersEncryptId_);
        public static LocalizedString MenuCiphersDecrypt() => new LocalizedString(MenuCiphersDecryptId_);

        // Compressions sub menu
        public static LocalizedString MenuCompressionsDecompress() => new LocalizedString(MenuCompressionsDecompressId_);
        public static LocalizedString MenuCompressionsCompress() => new LocalizedString(MenuCompressionsCompressId_);

        // Settings sub menu
        public static LocalizedString MenuSettingsIncludeDevBuilds() => new LocalizedString(MenuSettingsIncludeDevBuildsId_);
        public static LocalizedString MenuSettingsChangeLanguage() => new LocalizedString(MenuSettingsChangeLanguageId_);
        public static LocalizedString MenuSettingsChangeTheme() => new LocalizedString(MenuSettingsChangeThemeId_);

        // Change theme sub menu
        public static LocalizedString MenuSettingsChangeThemeDark() => new LocalizedString(MenuSettingsChangeThemeDarkId_);
        public static LocalizedString MenuSettingsChangeThemeLight() => new LocalizedString(MenuSettingsChangeThemeLightId_);

        // Update available dialog
        public static LocalizedString DialogUpdateAvailableCaption() => new LocalizedString(DialogUpdateAvailableCaptionId_);
        public static LocalizedString DialogUpdateAvailableText(string version, string build, string remoteVersion, string remoteBuild)
            => new LocalizedString(DialogUpdateAvailableTextId_, () => version, () => build, () => remoteVersion, () => remoteBuild);

        // Exception catched dialog
        public static LocalizedString DialogExceptionCatchedCaption() => new LocalizedString(DialogExceptionCatchedCaptionId_);

        // Plugins not available dialog
        public static LocalizedString DialogPluginsNotAvailableCaption() => new LocalizedString(DialogPluginsNotAvailableCaptionId_);
        public static LocalizedString DialogPluginsNotAvailableText() => new LocalizedString(DialogPluginsNotAvailableTextId_);

        // Load error dialog
        public static LocalizedString DialogLoadErrorCaption() => new LocalizedString(DialogLoadErrorCaptionId_);

        // Unsaved changes dialog
        public static LocalizedString DialogUnsavedChangesCaption() => new LocalizedString(DialogUnsavedChangesCaptionId_);
        public static LocalizedString DialogUnsavedChangesTextGeneric() => new LocalizedString(DialogUnsavedChangesTextGenericId_);
        public static LocalizedString DialogUnsavedChangesTextSpecific(UPath path) => new LocalizedString(DialogUnsavedChangesTextSpecificId_, () => path);

        // Dependant files dialog
        public static LocalizedString DialogDependantFilesCaption() => new LocalizedString(DialogDependantFilesCaptionId_);
        public static LocalizedString DialogDependantFilesText() => new LocalizedString(DialogDependantFilesTextId_);

        // Save error dialog
        public static LocalizedString DialogSaveErrorCaption() => new LocalizedString(DialogSaveErrorCaptionId_);

        // Status labels
        public static LocalizedString StatusPluginSelectNone() => new LocalizedString(StatusPluginSelectNoneId_);
        public static LocalizedString StatusPluginSelectUnknown(IPluginState state) => new LocalizedString(StatusPluginSelectUnknownId_, () => state.GetType().Name);
        public static LocalizedString StatusPluginLoadNone() => new LocalizedString(StatusPluginLoadNoneId_);
        public static LocalizedString StatusPluginLoadNoArchive() => new LocalizedString(StatusPluginLoadNoArchiveId_);
        public static LocalizedString StatusPluginStateInitError() => new LocalizedString(StatusPluginStateInitErrorId_);
        public static LocalizedString StatusPluginStateLoadNone() => new LocalizedString(StatusPluginStateLoadNoneId_);
        public static LocalizedString StatusPluginStateLoadError() => new LocalizedString(StatusPluginStateLoadErrorId_);
        public static LocalizedString StatusFileSelectNone() => new LocalizedString(StatusFileSelectNoneId_);
        public static LocalizedString StatusFileSelectInvalid() => new LocalizedString(StatusFileSelectInvalidId_);
        public static LocalizedString StatusFileLoadStart(UPath path) => new LocalizedString(StatusFileLoadStartId_, () => path);
        public static LocalizedString StatusFileLoadCancel() => new LocalizedString(StatusFileLoadCancelId_);
        public static LocalizedString StatusFileLoadOpening(UPath path) => new LocalizedString(StatusFileLoadOpeningId_, () => path);
        public static LocalizedString StatusFileLoadSaving(UPath path) => new LocalizedString(StatusFileLoadSavingId_, () => path);
        public static LocalizedString StatusFileLoadSuccess() => new LocalizedString(StatusFileLoadSuccessId_);
        public static LocalizedString StatusFileLoadFailure() => new LocalizedString(StatusFileLoadFailureId_);
        public static LocalizedString StatusFileLoadFailureWithPlugin(Guid id) => new LocalizedString(StatusFileLoadFailureWithPluginId_, () => id);
        public static LocalizedString StatusFileSaveStart(UPath path) => new LocalizedString(StatusFileSaveStartId_, () => path);
        public static LocalizedString StatusFileSaveClosed() => new LocalizedString(StatusFileSaveClosedId_);
        public static LocalizedString StatusFileSaveSaving(UPath path) => new LocalizedString(StatusFileSaveSavingId_, () => path);
        public static LocalizedString StatusFileSaveClosing(UPath path) => new LocalizedString(StatusFileSaveClosingId_, () => path);
        public static LocalizedString StatusFileSaveNotLoaded() => new LocalizedString(StatusFileSaveNotLoadedId_);
        public static LocalizedString StatusFileSaveNoChanges() => new LocalizedString(StatusFileSaveNoChangesId_);
        public static LocalizedString StatusFileSaveStateError() => new LocalizedString(StatusFileSaveStateErrorId_);
        public static LocalizedString StatusFileSaveStateReloadError() => new LocalizedString(StatusFileSaveStateReloadErrorId_);
        public static LocalizedString StatusFileSaveReplaceError() => new LocalizedString(StatusFileSaveReplaceErrorId_);
        public static LocalizedString StatusFileSaveCopyError() => new LocalizedString(StatusFileSaveCopyErrorId_);
        public static LocalizedString StatusFileSaveDestinationNotExist() => new LocalizedString(StatusFileSaveDestinationNotExistId_);
        public static LocalizedString StatusFileSaveSuccess() => new LocalizedString(StatusFileSaveSuccessId_);
        public static LocalizedString StatusFileSaveFailure() => new LocalizedString(StatusFileSaveFailureId_);
        public static LocalizedString StatusFileCloseStart(UPath path) => new LocalizedString(StatusFileCloseStartId_, () => path);
        public static LocalizedString StatusFileCloseCancel() => new LocalizedString(StatusFileCloseCancelId_);
        public static LocalizedString StatusFileCloseSaving(UPath path) => new LocalizedString(StatusFileCloseSavingId_, () => path);
        public static LocalizedString StatusFileCloseClosing(UPath path) => new LocalizedString(StatusFileCloseClosingId_, () => path);
        public static LocalizedString StatusFileCloseNotLoaded() => new LocalizedString(StatusFileCloseNotLoadedId_);
        public static LocalizedString StatusFileCloseSuccess() => new LocalizedString(StatusFileCloseSuccessId_);
        public static LocalizedString StatusOperationRunning() => new LocalizedString(StatusOperationRunningId_);

        // Error messages
        public static LocalizedString ErrorUnsupportedOperatingSystem(string os) => new LocalizedString(ErrorUnsupportedOperatingSystemId_, () => os);

        // File filters
        public static LocalizedString FilterAll() => new LocalizedString(FilterAllId_);
        public static LocalizedString FilterPng() => new LocalizedString(FilterPngId_);


        // Archive Form

        // File operations
        public static LocalizedString ArchiveFileExtract() => new LocalizedString(ArchiveFileExtractId_);
        public static LocalizedString ArchiveFileReplace() => new LocalizedString(ArchiveFileReplaceId_);
        public static LocalizedString ArchiveFileRename() => new LocalizedString(ArchiveFileRenameId_);
        public static LocalizedString ArchiveFileDelete() => new LocalizedString(ArchiveFileDeleteId_);

        // Folder operations
        public static LocalizedString ArchiveDirectoryExtract() => new LocalizedString(ArchiveDirectoryExtractId_);
        public static LocalizedString ArchiveDirectoryReplace() => new LocalizedString(ArchiveDirectoryReplaceId_);
        public static LocalizedString ArchiveDirectoryRename() => new LocalizedString(ArchiveDirectoryRenameId_);
        public static LocalizedString ArchiveDirectoryDelete() => new LocalizedString(ArchiveDirectoryDeleteId_);
        public static LocalizedString ArchiveDirectoryAdd() => new LocalizedString(ArchiveDirectoryAddId_);

        // Status labels
        public static LocalizedString ArchiveStatusExtractCancel() => new LocalizedString(ArchiveStatusExtractCancelId_);
        public static LocalizedString ArchiveStatusReplaceCancel() => new LocalizedString(ArchiveStatusReplaceCancelId_);
        public static LocalizedString ArchiveStatusRenameCancel() => new LocalizedString(ArchiveStatusRenameCancelId_);
        public static LocalizedString ArchiveStatusDeleteCancel() => new LocalizedString(ArchiveStatusDeleteCancelId_);
        public static LocalizedString ArchiveStatusAddCancel() => new LocalizedString(ArchiveStatusAddCancelId_);

        public static LocalizedString ArchiveStatusExtractSuccess() => new LocalizedString(ArchiveStatusExtractSuccessId_);
        public static LocalizedString ArchiveStatusReplaceSuccess() => new LocalizedString(ArchiveStatusReplaceSuccessId_);
        public static LocalizedString ArchiveStatusRenameSuccess() => new LocalizedString(ArchiveStatusRenameSuccessId_);
        public static LocalizedString ArchiveStatusDeleteSuccess() => new LocalizedString(ArchiveStatusDeleteSuccessId_);
        public static LocalizedString ArchiveStatusAddSuccess() => new LocalizedString(ArchiveStatusAddSuccessId_);

        public static LocalizedString ArchiveStatusRenameErrorNoName() => new LocalizedString(ArchiveStatusRenameErrorNoNameId_);
        public static LocalizedString ArchiveStatusAddError() => new LocalizedString(ArchiveStatusAddErrorId_);

        public static LocalizedString ArchiveStatusSelectNone() => new LocalizedString(ArchiveStatusSelectNoneId_);
        public static LocalizedString ArchiveStatusExtractNone() => new LocalizedString(ArchiveStatusExtractNoneId_);
        public static LocalizedString ArchiveStatusReplaceNone() => new LocalizedString(ArchiveStatusReplaceNoneId_);
        public static LocalizedString ArchiveStatusRenameNone() => new LocalizedString(ArchiveStatusRenameNoneId_);
        public static LocalizedString ArchiveStatusDeleteNone() => new LocalizedString(ArchiveStatusDeleteNoneId_);
        public static LocalizedString ArchiveStatusAddNone() => new LocalizedString(ArchiveStatusAddNoneId_);

        // Progress labels
        public static LocalizedString ArchiveProgressExtract() => new LocalizedString(ArchiveProgressExtractId_);
        public static LocalizedString ArchiveProgressReplace() => new LocalizedString(ArchiveProgressReplaceId_);
        public static LocalizedString ArchiveProgressRename() => new LocalizedString(ArchiveProgressRenameId_);
        public static LocalizedString ArchiveProgressDelete() => new LocalizedString(ArchiveProgressDeleteId_);
        public static LocalizedString ArchiveProgressAdd() => new LocalizedString(ArchiveProgressAddId_);

        // Rename dialog
        public static LocalizedString ArchiveDialogRenameFileCaption() => new LocalizedString(ArchiveDialogRenameFileCaptionId_);
        public static LocalizedString ArchiveDialogRenameDirectoryCaption() => new LocalizedString(ArchiveDialogRenameDirectoryCaptionId_);
        public static LocalizedString ArchiveDialogRenameText(string name) => new LocalizedString(ArchiveDialogRenameTextId_, () => name);

        // File headers
        public static LocalizedString ArchiveTableFilesName() => new LocalizedString(ArchiveTableFilesNameId_);
        public static LocalizedString ArchiveTableFilesSize() => new LocalizedString(ArchiveTableFilesSizeId_);

        // Search bar
        public static LocalizedString ArchiveSearchPlaceholder() => new LocalizedString(ArchiveSearchPlaceholderId_);
        public static LocalizedString ArchiveSearchClear() => new LocalizedString(ArchiveSearchClearId_);

        // Misc
        public static LocalizedString ArchiveFileCount(int fileCount) => new LocalizedString(ArchiveFileCountId_, () => fileCount);
        public static LocalizedString ArchiveCancelOperation() => new LocalizedString(ArchiveCancelOperationId_);


        // Image Form

        // Menu
        public static LocalizedString ImageMenuExport() => new LocalizedString(ImageMenuExportId_);
        public static LocalizedString ImageMenuImport() => new LocalizedString(ImageMenuImportId_);
        public static LocalizedString ImageMenuExportPng() => new LocalizedString(ImageMenuExportPngId_);
        public static LocalizedString ImageMenuImportPng() => new LocalizedString(ImageMenuImportPngId_);

        // Labels
        public static LocalizedString ImageLabelWidth() => new LocalizedString(ImageLabelWidthId_);
        public static LocalizedString ImageLabelHeight() => new LocalizedString(ImageLabelHeightId_);
        public static LocalizedString ImageLabelFormat() => new LocalizedString(ImageLabelFormatId_);
        public static LocalizedString ImageLabelPalette() => new LocalizedString(ImageLabelPaletteId_);

        // Status
        public static LocalizedString ImageStatusImportSuccess() => new LocalizedString(ImageStatusImportSuccessId_);

        // Progress
        public static LocalizedString ImageProgressDecode() => new LocalizedString(ImageProgressDecodeId_);


        // Dialogs

        // Dialog manager
        public static LocalizedString DialogManagerButtonOk() => new LocalizedString(DialogManagerButtonOkId_);

        // Choose plugin dialog
        public static LocalizedString DialogChoosePluginCaption() => new LocalizedString(DialogChoosePluginCaptionId_);

        public static LocalizedString DialogChoosePluginHeaderGeneric() => new LocalizedString(DialogChoosePluginHeaderGenericId_);
        public static LocalizedString DialogChoosePluginHeaderIdentificationNone() => new LocalizedString(DialogChoosePluginHeaderIdentificationNoneId_);
        public static LocalizedString DialogChoosePluginHeaderIdentificationMultiple() => new LocalizedString(DialogChoosePluginHeaderIdentificationMultipleId_);
        public static LocalizedString DialogChoosePluginHeaderIdentificationNote() => new LocalizedString(DialogChoosePluginHeaderIdentificationNoteId_);

        public static LocalizedString DialogChoosePluginPluginsTableName() => new LocalizedString(DialogChoosePluginPluginsTableNameId_);
        public static LocalizedString DialogChoosePluginPluginsTableType() => new LocalizedString(DialogChoosePluginPluginsTableTypeId_);
        public static LocalizedString DialogChoosePluginPluginsTableDescription() => new LocalizedString(DialogChoosePluginPluginsTableDescriptionId_);
        public static LocalizedString DialogChoosePluginPluginsTableId() => new LocalizedString(DialogChoosePluginPluginsTableIdId_);

        public static LocalizedString DialogChoosePluginContinue() => new LocalizedString(DialogChoosePluginContinueId_);
        public static LocalizedString DialogChoosePluginViewRaw() => new LocalizedString(DialogChoosePluginViewRawId_);
        public static LocalizedString DialogChoosePluginCancel() => new LocalizedString(DialogChoosePluginCancelId_);
        public static LocalizedString DialogChoosePluginShowAll() => new LocalizedString(DialogChoosePluginShowAllId_);

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
