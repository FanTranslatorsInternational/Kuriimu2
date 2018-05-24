using Caliburn.Micro;
using Kontract.Interface;
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
            DisplayName = "Kuriimu";
            _kore = new Kore.Kore();
        }

        public void OpenButton()
        {
            var ofd = new OpenFileDialog { Filter = _kore.FileFilters };
            if (ofd.ShowDialog() != true) return;

            var kfi = _kore.LoadFile(ofd.FileName);
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

        public void SaveButton()
        {
            switch (ActiveItem)
            {
                case TextEditor2ViewModel txt2:
                    txt2.Save();
                    break;
            }
        }

        public void SaveAsButton()
        {
            var filter = "Any File (*.*)|*.*";

            switch (ActiveItem)
            {
                case TextEditor2ViewModel txt2:
                    filter = txt2.KoreFile.Filter;
                    break;
            }

            var sfd = new SaveFileDialog { Filter = filter };

            if (sfd.ShowDialog() == true)
            {
                switch (ActiveItem)
                {
                    case TextEditor2ViewModel txt2:
                        txt2.Save(sfd.FileName);
                        break;
                }
            }
        }

        public void DebugButton()
        {
            _kore.Debug();
        }

        public void CloseTab(Screen tab)
        {
            tab.TryClose();
        }
    }
}
