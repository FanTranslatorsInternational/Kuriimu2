using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Font;
using Kore.Managers.Plugins;
using Kore.Progress;
using Kuriimu2.Wpf.Interfaces;
using Microsoft.Win32;

namespace Kuriimu2.Wpf.ViewModels
{
    public sealed class ShellViewModel : Conductor<IScreen>.Collection.OneActive
    {
        #region Private

        private IWindowManager _wm = new WindowManager();
        private List<IScreen> _windows = new List<IScreen>();
        private FileManager _pluginManager;

        #endregion

        public ShellViewModel()
        {
            DisplayName = "Kuriimu2.Wpf";

            // Assign plugin loading event handler.
            _pluginManager = new FileManager("plugins") { Progress = new ProgressContext(new NullProgressOutput()) };

            // TODO: Add event for failed identification
            //_pluginManager.IdentificationFailed += FileIdentificationFailed;

            // Load passed-in file
            // TODO: Somehow handle multiple files via delayed asynchronous loading
            if (AppBootstrapper.Args.Length > 0 && File.Exists(AppBootstrapper.Args[0]))
                LoadFile(AppBootstrapper.Args[0]);
        }

        //private void FileIdentificationFailed(object sender, IdentificationFailedEventArgs e)
        //{
        //    var pe = new SelectAdapterViewModel(e.BlindAdapters.ToList(), _pluginManager, _pluginLoader, e.FileName);
        //    _windows.Add(pe);

        //    if (_wm.ShowDialogAsync(pe).Result == true)
        //    {
        //        e.SelectedAdapter = pe.Adapter;

        //        if (pe.RememberMySelection)
        //        {
        //            // TODO: Do magic
        //        }
        //    }
        //}

        public void ExitMenu()
        {
            if ((ActiveItem as IFileEditor)?.KoreFile.StateChanged ?? false)
                ; //ConfirmLossOfChanges();
            Application.Current.Shutdown();
        }

        public async void OpenButton()
        {
            // TODO: Add filters from loaded plugins
            var ofd = new OpenFileDialog { Filter = ""/*_pluginManager.FileFilters*/, Multiselect = true };
            if (ofd.ShowDialog() != true) return;

            foreach (var file in ofd.FileNames)
                await LoadFile(file);
        }

        //public async void OpenTypeButton()
        //{
        //    var pe = new OpenTypeViewModel(_pluginManager)
        //    {
        //        Title = "Open File by Type",
        //        Message = ""
        //    };
        //    _windows.Add(pe);

        //    if (_wm.ShowDialog(pe) == true)
        //    {
        //        await LoadFile(pe.SelectedFilePath, pe.SelectedFormatType);
        //    }
        //}

        public async void FileDrop(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null) return;

            foreach (var file in files)
                await LoadFile(file);
        }

        public bool SaveButtonsEnabled => (ActiveItem as IFileEditor)?.KoreFile.PluginState is ISaveFiles;

        public void SaveButton()
        {
            SaveFile();
        }

        public void SaveAsButton()
        {
            var filter = "Any File (*.*)|*.*";

            if (ActiveItem is IFileEditor editor)
            {
                // TODO: Set extension from loaded plugin
                //filter = editor.KoreFile.Filter;

                var sfd = new SaveFileDialog { FileName = editor.KoreFile.FilePath.FullName, Filter = filter };
                if (sfd.ShowDialog() != true) return;

                SaveFile(sfd.FileName);
            }
        }

        public void DebugButton()
        {
            //_pluginManager.Debug();
        }

        #region ToolBar Visibility

        // Text
        public Visibility TextEditorToolsVisible => ActiveItem is ITextEditor ? Visibility.Visible : Visibility.Hidden;
        public Visibility TextEditorCanExportFiles => ActiveItem is ITextEditor text ? (text.TextEditorCanExportFiles ? Visibility.Visible : Visibility.Hidden) : Visibility.Hidden;
        public Visibility TextEditorCanImportFiles => ActiveItem is ITextEditor text ? (text.TextEditorCanImportFiles ? Visibility.Visible : Visibility.Hidden) : Visibility.Hidden;

        #endregion

        public void TextEditorExportFile()
        {
            var editor = (IFileEditor)ActiveItem;
            if (!(editor.KoreFile.PluginState is ITextState)) return;

            // TODO: Get text adapters
            //var creators = _pluginLoader.GetAdapters<ITextAdapter>().Where(a => a is ICreateFiles && a is IAddEntries);

            var sfd = new SaveFileDialog
            {
                // TODO: Create correct save file dialog
                FileName = Path.GetFileName(editor.KoreFile.FilePath.FullName) + ".ext"/*Common.GetAdapterExtension(creators.First())*/,
                InitialDirectory = Path.GetDirectoryName(editor.KoreFile.FilePath.FullName),
                // TODO: Re-enable filter
                //Filter = Common.GetAdapterFilters(new List<string>())
            };
            if (sfd.ShowDialog() != true) return;

            // TODO: Re-enable export file method
            //Text.ExportFile(adapter, creators.Skip(sfd.FilterIndex - 1).First(), sfd.FileName);
        }

        // TODO: Re-enable import file
        public void TextEditorImportFile()
        {
            //var editor = (IFileEditor)ActiveItem;
            //if (!(editor.KoreFile.PluginState is ITextAdapter adapter)) return;

            //var ofd = new OpenFileDialog { Filter = _pluginManager.FileFiltersByType<ITextAdapter>("All Supported Text Files") };
            //if (ofd.ShowDialog() != true) return;

            //editor.KoreFile.HasChanges = _pluginManager.ImportFile(adapter, ofd.FileName);
        }

        // Tabs
        public void TabChanged(SelectionChangedEventArgs args)
        {
            // General
            NotifyOfPropertyChange(() => SaveButtonsEnabled);

            // Text Editor
            NotifyOfPropertyChange(() => TextEditorToolsVisible);
            NotifyOfPropertyChange(() => TextEditorCanExportFiles);
            NotifyOfPropertyChange(() => TextEditorCanImportFiles);
        }

        public void CloseTab(IScreen tab)
        {
            tab.TryCloseAsync();
            switch (tab)
            {
                case IFileEditor editor:
                    // TODO: Close file properly
                    //_pluginManager.CloseFile(editor.KoreFile);
                    break;
            }
        }

        public void CloseAllTabs()
        {
            for (var i = Items.Count - 1; i >= 0; i--)
                CloseTab(Items[i]);
        }

        #region Private Methods

        private async Task<bool> LoadFile(string filename)
        {
            IFileState kfi = null;

            var loadResult = await _pluginManager.LoadFile(filename, Guid.Parse("b1b397c4-9a02-4828-b568-39cad733fa3a"));
            if (!loadResult.IsSuccessful)
            {
#if DEBUG
                MessageBox.Show(loadResult.Exception.ToString(), "Open File", MessageBoxButton.OK, MessageBoxImage.Error);
#else
                MessageBox.Show(loadResult.Message, "Open File", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
                return false;
            }

            kfi = loadResult.LoadedFileState;

            ActivateTab(kfi);

            return true;
        }

        private void ActivateTab(IFileState kfi)
        {
            if (kfi == null) return;

            switch (kfi.PluginState)
            {
                case ITextState txt2:
                    ActivateItemAsync(new TextEditor2ViewModel(_pluginManager, kfi), new System.Threading.CancellationToken());
                    break;

                case IImageState img:
                    ActivateItemAsync(new ImageEditorViewModel(_pluginManager, kfi), new System.Threading.CancellationToken());
                    break;

                case IFontState fnt:
                    ActivateItemAsync(new FontEditorViewModel(kfi), new System.Threading.CancellationToken());
                    break;
            }
        }

        /// <summary>
        /// The global save method that handles the various editor types.
        /// </summary>
        /// <param name="filename">The target file name to save as.</param>
        private async void SaveFile(string filename = "")
        {
            var currentTab = ActiveItem as IFileEditor;
            try
            {
                if (currentTab == null)
                    return;

                if (!currentTab.KoreFile.StateChanged && filename == string.Empty)
                    return;

                var saveResult = await _pluginManager.SaveFile(currentTab.KoreFile, filename);
                if (!saveResult.IsSuccessful)
                {
#if DEBUG
                    MessageBox.Show(saveResult.Exception.ToString(), "Save File", MessageBoxButton.OK, MessageBoxImage.Error);
#else
                    MessageBox.Show(saveResult.Message, "Save File", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
                    return;
                }

                // Handle archive editors.
                // TODO: Port the win forms code for this behaviour to WPF MVVM
                if (ActiveItem is IArchiveEditor archiveEditor)
                {
                    //archiveEditor.UpdateChildTabs(savedKfi);
                    //archiveEditor.UpdateParent();
                }
            }
            catch (Exception ex)
            {
                // ignored
            }
        }

        #endregion

        public override Task TryCloseAsync(bool? dialogResult = null)
        {
            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                var scr = _windows[i];
                scr.TryCloseAsync(dialogResult);
                _windows.Remove(scr);
            }
            return base.TryCloseAsync(dialogResult);
        }
    }
}
