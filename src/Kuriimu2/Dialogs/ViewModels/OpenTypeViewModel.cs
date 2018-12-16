using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kuriimu2.Dialogs.Common;
using Microsoft.Win32;

namespace Kuriimu2.Dialogs.ViewModels
{
    public sealed class OpenTypeViewModel : Screen
    {
        private readonly Kore.Kore _kore;

        private string _selectedPluginType;
        private ILoadFiles _selectedFormatType;
        private string _selectedFilePath;

        public string Title { get; set; } = "Open Type";
        public BitmapImage Icon { get; private set; }
        public string Message { get; set; } = "";
        public string Error { get; set; } = string.Empty;
        public int TextBoxWidth { get; set; } = 200;

        public Func<ValidationResult> ValidationCallback;

        public OpenTypeViewModel(Kore.Kore kore)
        {
            _kore = kore;

            SelectedPluginType = nameof(ITextAdapter);
        }

        public List<string> PluginTypes => _kore.GetFileLoadingAdapterNames();

        public string SelectedPluginType
        {
            get => _selectedPluginType;
            set
            {
                _selectedPluginType = value;
                NotifyOfPropertyChange(() => SelectedPluginType);
                NotifyOfPropertyChange(() => FormatTypes);
            }
        }

        public List<ILoadFiles> FormatTypes
        {
            get
            {
                if (SelectedPluginType == null) return null;

                switch (SelectedPluginType)
                {
                    case nameof(ITextAdapter):
                        return _kore.GetAdapters<ITextAdapter>().Cast<ILoadFiles>().ToList();
                    case nameof(IImageAdapter):
                        return _kore.GetAdapters<IImageAdapter>().Cast<ILoadFiles>().ToList();
                    case nameof(IFontAdapter):
                        return _kore.GetAdapters<IFontAdapter>().Cast<ILoadFiles>().ToList();
                    default:
                        return null;
                }
            }
        }

        public ILoadFiles SelectedFormatType
        {
            get => _selectedFormatType;
            set
            {
                _selectedFormatType = value;
                NotifyOfPropertyChange(() => SelectedFormatType);
            }
        }

        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set
            {
                if (value == _selectedFilePath) return;
                _selectedFilePath = value;
                NotifyOfPropertyChange(() => SelectedFilePath);
            }
        }

        public void SelectFileButton()
        {
            if (SelectedFormatType == null) return;

            var ofd = new OpenFileDialog { Filter = Kore.Utilities.Common.GetAdapterFilter(SelectedFormatType) };
            if (ofd.ShowDialog() != true) return;

            SelectedFilePath = ofd.FileName;
        }

        public void OKButton()
        {
            // Set output variables

            if (ValidationCallback != null)
            {
                var results = ValidationCallback();

                if (results.CanClose)
                    TryClose(true);
                else
                {
                    Error = results.ErrorMessage;
                    NotifyOfPropertyChange(() => Error);
                }
            }
            else
            {
                TryClose(true);
            }
        }
    }
}
