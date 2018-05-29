using System.IO;
using Caliburn.Micro;
using Kontract.Interfaces;
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

            if (AppBootstrapper.Args.Length > 0 && File.Exists(AppBootstrapper.Args[0]))
                LoadFile(AppBootstrapper.Args[0]);
        }

        public void OpenButton()
        {
            var ofd = new OpenFileDialog { Filter = _kore.FileFilters };
            if (ofd.ShowDialog() != true) return;

            LoadFile(ofd.FileName);
        }

        public void SaveButton()
        {
            SaveFile();
        }

        public void SaveAsButton()
        {
            var filter = "Any File (*.*)|*.*";

            if (ActiveItem is IEditor editor)
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

        public bool SaveButtonsEnabled() => (ActiveItem as IEditor)?.KoreFile.Adapter is ISaveFiles;

        public void DebugButton()
        {
            _kore.Debug();
        }

        public void CloseTab(Screen tab)
        {
            tab.TryClose();
            switch (ActiveItem)
            {
                case TextEditor2ViewModel txt2:
                    _kore.CloseFile(txt2.KoreFile);
                    break;
                case FontEditorViewModel fnt:
                    _kore.CloseFile(fnt.KoreFile);
                    break;
            }
        }

        #region Methods

        private void LoadFile(string filename)
        {
            var kfi = _kore.LoadFile(filename);
            switch (kfi.Adapter)
            {
                case ITextAdapter txt2:
                    ActivateItem(new TextEditor2ViewModel(kfi));
                    break;
                case IFontAdapter fnt:
                    ActivateItem(new FontEditorViewModel(kfi));
                    break;
            }
        }

        private void SaveFile(string filename = "")
        {
            (ActiveItem as IEditor)?.Save(filename);
        }

        #endregion
    }
}
