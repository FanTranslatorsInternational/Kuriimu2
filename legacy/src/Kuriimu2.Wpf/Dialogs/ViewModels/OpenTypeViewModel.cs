using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Text;
using Kore.Managers.Plugins;
using Kuriimu2.Wpf.Dialogs.Common;
using Microsoft.Win32;

namespace Kuriimu2.Wpf.Dialogs.ViewModels
{
    public sealed class OpenTypeViewModel : Screen
    {
        private readonly FileManager _pluginManager;

        private string _selectedPluginType;
        private ILoadFiles _selectedFormatType;
        private string _selectedFilePath;

        public string Title { get; set; } = "Open Type";
        public BitmapImage Icon { get; private set; }
        public string Message { get; set; } = "";
        public string Error { get; set; } = string.Empty;
        public int TextBoxWidth { get; set; } = 200;

        public Func<ValidationResult> ValidationCallback;

        public OpenTypeViewModel(FileManager pluginManager)
        {
            _pluginManager = pluginManager;

            SelectedPluginType = nameof(ITextState);
        }

        // TODO: Get file loading adapter names
        public List<string> PluginTypes => new List<string>(); //_pluginManager.GetFileLoadingAdapterNames();

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

        // TODO: Return format types
        public List<ILoadFiles> FormatTypes
        {
            get
            {
                if (SelectedPluginType == null) 
                    return null;

                return null;

                //switch (SelectedPluginType)
                //{
                //    case nameof(ITextAdapter):
                //        return _pluginLoader.GetAdapters<ITextAdapter>().Cast<ILoadFiles>().ToList();
                //    case nameof(IImageAdapter):
                //        return _pluginLoader.GetAdapters<IImageAdapter>().Cast<ILoadFiles>().ToList();
                //    case nameof(IFontAdapter):
                //        return _pluginLoader.GetAdapters<IFontAdapter>().Cast<ILoadFiles>().ToList();
                //    default:
                //        return null;
                //}
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

            // TODO: Add filter
            var ofd = new OpenFileDialog { /*Filter = Kore.Utilities.Common.GetAdapterFilter(SelectedFormatType)*/ };
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
                    TryCloseAsync(true);
                else
                {
                    Error = results.ErrorMessage;
                    NotifyOfPropertyChange(() => Error);
                }
            }
            else
            {
                TryCloseAsync(true);
            }
        }
    }
}
