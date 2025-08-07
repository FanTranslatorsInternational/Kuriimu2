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
        public static LocalizedString ApplicationName => LocalizedString.FromId("Application.Name");

        // Menus
        public static LocalizedString MenuFile => LocalizedString.FromId("Menu.File");
        public static LocalizedString MenuTools => LocalizedString.FromId("Menu.Tools");
        public static LocalizedString MenuSettings => LocalizedString.FromId("Menu.Settings");
        public static LocalizedString MenuHelp => LocalizedString.FromId("Menu.Help");

        // File Menu
        public static LocalizedString MenuFileOpen => LocalizedString.FromId("Menu.File.Open");
        public static LocalizedString MenuFileOpenShortcut => LocalizedString.FromId("Menu.File.Open.Shortcut");
        public static LocalizedString MenuFileOpenWith => LocalizedString.FromId("Menu.File.OpenWith");
        public static LocalizedString MenuFileOpenWithShortcut => LocalizedString.FromId("Menu.File.OpenWith.Shortcut");
        public static LocalizedString MenuFileSave => LocalizedString.FromId("Menu.File.Save");
        public static LocalizedString MenuFileSaveShortcut => LocalizedString.FromId("Menu.File.Save.Shortcut");
        public static LocalizedString MenuFileSaveAs => LocalizedString.FromId("Menu.File.SaveAs");
        public static LocalizedString MenuFileSaveAsShortcut => LocalizedString.FromId("Menu.File.SaveAs.Shortcut");
        public static LocalizedString MenuFileSaveAll => LocalizedString.FromId("Menu.File.SaveAll");
        public static LocalizedString MenuFileSaveAllShortcut => LocalizedString.FromId("Menu.File.SaveAll.Shortcut");
        public static LocalizedString MenuFileClose => LocalizedString.FromId("Menu.File.Close");

        // Tools Menu
        public static LocalizedString MenuToolsCiphers => LocalizedString.FromId("Menu.Tools.Ciphers");
        public static LocalizedString MenuToolsCompressions => LocalizedString.FromId("Menu.Tools.Compressions");
        public static LocalizedString MenuToolsImageTranscoder => LocalizedString.FromId("Menu.Tools.ImageTranscoder");
        public static LocalizedString MenuToolsRawImageViewer => LocalizedString.FromId("Menu.Tools.RawImageViewer");
        public static LocalizedString MenuToolsTextSequenceSearcher => LocalizedString.FromId("Menu.Tools.TextSequenceSearcher");
        public static LocalizedString MenuToolsBatchExtractor => LocalizedString.FromId("Menu.Tools.BatchExtractor");
        public static LocalizedString MenuToolsBatchInjector => LocalizedString.FromId("Menu.Tools.BatchInjector");
        public static LocalizedString MenuToolsHashes => LocalizedString.FromId("Menu.Tools.Hashes");

        // Settings Menu
        public static LocalizedString MenuSettingsIncludeDevBuilds => LocalizedString.FromId("Menu.Settings.IncludeDevBuilds");
        public static LocalizedString MenuSettingsChangeLanguage => LocalizedString.FromId("Menu.Settings.ChangeLanguage");
        public static LocalizedString MenuSettingsChangeTheme => LocalizedString.FromId("Menu.Settings.ChangeTheme");

        // Theme Menu
        public static LocalizedString MenuSettingsChangeThemeDark => LocalizedString.FromId("Menu.Settings.ChangeTheme.Dark");
        public static LocalizedString MenuSettingsChangeThemeLight => LocalizedString.FromId("Menu.Settings.ChangeTheme.Light");

        // Ciphers Menu
        public static LocalizedString MenuToolsCiphersCaption => LocalizedString.FromId("Menu.Tools.Ciphers.Caption");
        public static LocalizedString MenuToolsCiphersEncrypt => LocalizedString.FromId("Menu.Tools.Ciphers.Encrypt");
        public static LocalizedString MenuToolsCiphersDecrypt => LocalizedString.FromId("Menu.Tools.Ciphers.Decrypt");
        public static LocalizedString MenuToolsCiphersExecute => LocalizedString.FromId("Menu.Tools.Ciphers.Execute");
        public static LocalizedString MenuToolsCiphersInputFolder => LocalizedString.FromId("Menu.Tools.Ciphers.Input.Folder");
        public static LocalizedString MenuToolsCiphersInputFile => LocalizedString.FromId("Menu.Tools.Ciphers.Input.File");
        public static LocalizedString MenuToolsCiphersInputSubDirectories => LocalizedString.FromId("Menu.Tools.Ciphers.Input.SubDirectories");
        public static LocalizedString MenuToolsCiphersInputParameters => LocalizedString.FromId("Menu.Tools.Ciphers.Input.Parameters");
        public static LocalizedString MenuToolsCiphersLogProcess(string filePath)
            => LocalizedString.FromId("Menu.Tools.Ciphers.Log.Process", () => filePath);
        public static LocalizedString MenuToolsCiphersLogError(string filePath)
            => LocalizedString.FromId("Menu.Tools.Ciphers.Log.Error", () => filePath);
        public static LocalizedString MenuToolsCiphersProgress => LocalizedString.FromId("Menu.Tools.Ciphers.Progress");

        // Compressions Menu
        public static LocalizedString MenuToolsCompressionsCaption => LocalizedString.FromId("Menu.Tools.Compressions.Caption");
        public static LocalizedString MenuToolsCompressionsDecompress => LocalizedString.FromId("Menu.Tools.Compressions.Decompress");
        public static LocalizedString MenuToolsCompressionsCompress => LocalizedString.FromId("Menu.Tools.Compressions.Compress");
        public static LocalizedString MenuToolsCompressionsExecute => LocalizedString.FromId("Menu.Tools.Compressions.Execute");
        public static LocalizedString MenuToolsCompressionsInputFolder => LocalizedString.FromId("Menu.Tools.Compressions.Input.Folder");
        public static LocalizedString MenuToolsCompressionsInputFile => LocalizedString.FromId("Menu.Tools.Compressions.Input.File");
        public static LocalizedString MenuToolsCompressionsInputSubDirectories => LocalizedString.FromId("Menu.Tools.Compressions.Input.SubDirectories");
        public static LocalizedString MenuToolsCompressionsLogProcess(string filePath) 
            => LocalizedString.FromId("Menu.Tools.Compressions.Log.Process", () => filePath);
        public static LocalizedString MenuToolsCompressionsLogError(string filePath)
            => LocalizedString.FromId("Menu.Tools.Compressions.Log.Error", () => filePath);
        public static LocalizedString MenuToolsCompressionsProgress => LocalizedString.FromId("Menu.Tools.Compressions.Progress");

        // Image Transcoder
        public static LocalizedString MenuToolsImageTranscoderCaption => LocalizedString.FromId("Menu.Tools.ImageTranscoder.Caption");
        public static LocalizedString MenuToolsImageTranscoderFile => LocalizedString.FromId("Menu.Tools.ImageTranscoder.File");
        public static LocalizedString MenuToolsImageTranscoderFileOpen => LocalizedString.FromId("Menu.Tools.ImageTranscoder.File.Open");
        public static LocalizedString MenuToolsImageTranscoderFileOpenShortcut => LocalizedString.FromId("Menu.Tools.ImageTranscoder.File.Open.Shortcut");
        public static LocalizedString MenuToolsImageTranscoderEncoding => LocalizedString.FromId("Menu.Tools.ImageTranscoder.Encoding");
        public static LocalizedString MenuToolsImageTranscoderPaletteEncoding => LocalizedString.FromId("Menu.Tools.ImageTranscoder.PaletteEncoding");
        public static LocalizedString MenuToolsImageTranscoderQuantizer => LocalizedString.FromId("Menu.Tools.ImageTranscoder.Quantizer");
        public static LocalizedString MenuToolsImageTranscoderColorCache => LocalizedString.FromId("Menu.Tools.ImageTranscoder.ColorCache");
        public static LocalizedString MenuToolsImageTranscoderDitherer => LocalizedString.FromId("Menu.Tools.ImageTranscoder.Ditherer");
        public static LocalizedString MenuToolsImageTranscoderNoDitherer => LocalizedString.FromId("Menu.Tools.ImageTranscoder.NoDitherer");
        public static LocalizedString MenuToolsImageTranscoderColorCount => LocalizedString.FromId("Menu.Tools.ImageTranscoder.ColorCount");

        // Raw Image Viewer
        public static LocalizedString MenuToolsRawImageViewerCaption => LocalizedString.FromId("Menu.Tools.RawImageViewer.Caption");
        public static LocalizedString MenuToolsRawImageViewerFile => LocalizedString.FromId("Menu.Tools.RawImageViewer.File");
        public static LocalizedString MenuToolsRawImageViewerFileOpen => LocalizedString.FromId("Menu.Tools.RawImageViewer.File.Open");
        public static LocalizedString MenuToolsRawImageViewerFileOpenShortcut => LocalizedString.FromId("Menu.Tools.RawImageViewer.File.Open.Shortcut");
        public static LocalizedString MenuToolsRawImageViewerPlaceholder => LocalizedString.FromId("Menu.Tools.RawImageViewer.Placeholder");
        public static LocalizedString MenuToolsRawImageViewerWidth => LocalizedString.FromId("Menu.Tools.RawImageViewer.Width");
        public static LocalizedString MenuToolsRawImageViewerHeight => LocalizedString.FromId("Menu.Tools.RawImageViewer.Height");
        public static LocalizedString MenuToolsRawImageViewerOffset => LocalizedString.FromId("Menu.Tools.RawImageViewer.Offset");
        public static LocalizedString MenuToolsRawImageViewerPaletteOffset => LocalizedString.FromId("Menu.Tools.RawImageViewer.PaletteOffset");
        public static LocalizedString MenuToolsRawImageViewerEncoding => LocalizedString.FromId("Menu.Tools.RawImageViewer.Encoding");
        public static LocalizedString MenuToolsRawImageViewerPaletteEncoding => LocalizedString.FromId("Menu.Tools.RawImageViewer.PaletteEncoding");
        public static LocalizedString MenuToolsRawImageViewerEncodingComponentOrder => LocalizedString.FromId("Menu.Tools.RawImageViewer.EncodingComponentOrder");
        public static LocalizedString MenuToolsRawImageViewerPaletteEncodingComponentOrder => LocalizedString.FromId("Menu.Tools.RawImageViewer.PaletteEncodingComponentOrder");
        public static LocalizedString MenuToolsRawImageViewerSwizzle => LocalizedString.FromId("Menu.Tools.RawImageViewer.Swizzle");
        public static LocalizedString MenuToolsRawImageViewerNoSwizzle => LocalizedString.FromId("Menu.Tools.RawImageViewer.NoSwizzle");
        public static LocalizedString MenuToolsRawImageViewerCustomSwizzle => LocalizedString.FromId("Menu.Tools.RawImageViewer.CustomSwizzle");
        public static LocalizedString MenuToolsRawImageViewerRenderSwizzle => LocalizedString.FromId("Menu.Tools.RawImageViewer.RenderSwizzle");
        public static LocalizedString MenuToolsRawImageViewerSwizzleEditorAddControl => LocalizedString.FromId("Menu.Tools.RawImageViewer.SwizzleEditor.AddControl");
        public static LocalizedString MenuToolsRawImageViewerSwizzleEditorRemoveControl => LocalizedString.FromId("Menu.Tools.RawImageViewer.SwizzleEditor.RemoveControl");
        public static LocalizedString MenuToolsRawImageViewerSwizzleEditorMoveControl => LocalizedString.FromId("Menu.Tools.RawImageViewer.SwizzleEditor.MoveControl");

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
        public static LocalizedString ArchiveFileDeleteShortcut => LocalizedString.FromId("Archive.File.Delete.Shortcut");

        // Folder Operations
        public static LocalizedString ArchiveDirectoryExtract => LocalizedString.FromId("Archive.Directory.Extract");
        public static LocalizedString ArchiveDirectoryReplace => LocalizedString.FromId("Archive.Directory.Replace");
        public static LocalizedString ArchiveDirectoryRename => LocalizedString.FromId("Archive.Directory.Rename");
        public static LocalizedString ArchiveDirectoryDelete => LocalizedString.FromId("Archive.Directory.Delete");
        public static LocalizedString ArchiveDirectoryDeleteShortcut => LocalizedString.FromId("Archive.Directory.Delete.Shortcut");
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
        public static LocalizedString FontSearchPlaceholder => LocalizedString.FromId("Font.Search.Placeholder");
        public static LocalizedString FontPreviewPlaceholder=> LocalizedString.FromId("Font.Preview.Placeholder");

        // Generate
        public static LocalizedString FontGenerateCaption => LocalizedString.FromId("Font.Generate.Caption");
        public static LocalizedString FontGenerateDefaultCharacters => LocalizedString.FromId("Font.Generate.DefaultCharacters");

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
