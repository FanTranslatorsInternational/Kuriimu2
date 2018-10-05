using System;
using System.IO;
using System.Windows;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kore;
using Kuriimu2.Interface;
using Microsoft.Win32;

namespace Kuriimu2.ViewModels
{
    public sealed class ShellViewModel : Conductor<IScreen>.Collection.OneActive
    {
        #region Private

        private Kore.Kore _kore;

        #endregion

        public ShellViewModel()
        {
            DisplayName = "Kuriimu2";
            _kore = new Kore.Kore();

            // Load passed-in file
            if (AppBootstrapper.Args.Length > 0 && File.Exists(AppBootstrapper.Args[0]))
                LoadFile(AppBootstrapper.Args[0]);
        }

        public void OpenButton()
        {
            var ofd = new OpenFileDialog { Filter = _kore.FileFilters };
            if (ofd.ShowDialog() != true) return;

            LoadFile(ofd.FileName);
        }

        public bool SaveButtonsEnabled() => (ActiveItem as IFileEditor)?.KoreFile.Adapter is ISaveFiles;

        public void SaveButton()
        {
            SaveFile();
        }

        public void SaveAsButton()
        {
            var filter = "Any File (*.*)|*.*";

            if (ActiveItem is IFileEditor editor)
            {
                filter = editor.KoreFile.Filter;

                var sfd = new SaveFileDialog { FileName = editor.KoreFile.FileInfo.Name, Filter = filter };
                if (sfd.ShowDialog() != true) return;

                SaveFile(sfd.FileName);
            }
            else
            {

            }
        }

        public void DebugButton()
        {
            _kore.Debug();
        }

        public void CloseTab(IScreen tab)
        {
            tab.TryClose();
            switch (tab)
            {
                case IFileEditor editor:
                    _kore.CloseFile(editor.KoreFile);
                    break;
            }
        }

        public void CloseAllTabs()
        {
            for (var i = Items.Count - 1; i >= 0; i--)
                CloseTab(Items[i]);
        }

        #region Private Methods

        private void LoadFile(string filename)
        {
            KoreFileInfo kfi = null;

            try
            {
                kfi = _kore.LoadFile(filename);
            }
            catch (LoadFileException ex)
            {
                MessageBox.Show(ex.ToString(), "Open File", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (kfi != null)
                switch (kfi.Adapter)
                {
                    case ITextAdapter txt2:
                        //var dr = MessageBox.Show("Use V1 Editor?", "Editor Selection", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        //if (dr == MessageBoxResult.Yes)
                        //    ActivateItem(new TextEditor1ViewModel(kfi));
                        //else
                            ActivateItem(new TextEditor2ViewModel(kfi));
                        break;
                    case IImageAdapter img:
                        ActivateItem(new ImageEditorViewModel(kfi));
                        break;
                    case IFontAdapter fnt:
                        ActivateItem(new FontEditorViewModel(kfi));
                        break;
                }
        }

        private void SaveFile(string filename = "")
        {
            (ActiveItem as IFileEditor)?.Save(filename);
        }

        #endregion
    }
}
