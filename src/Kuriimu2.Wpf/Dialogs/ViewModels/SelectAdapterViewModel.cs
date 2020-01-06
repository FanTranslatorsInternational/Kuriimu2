using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Caliburn.Micro;
using Kontract.Attributes;
using Kontract.Interfaces.Plugins.State;
using Kore.Managers.Plugins;
using Kuriimu2.Wpf.Dialogs.Common;

namespace Kuriimu2.Wpf.Dialogs.ViewModels
{
    public sealed class SelectAdapterViewModel : Screen
    {
        //private string _fileName;
        private SelectableAdapter _selectedAdapter;
        private string _rememberMySelectionText;

        public string Title { get; set; } = "Select A Plugin";
        public string Message { get; set; } = "";
        public string Error { get; set; } = string.Empty;
        public int TextBoxWidth { get; set; } = 200;

        public List<SelectableAdapter> Adapters { get; } = new List<SelectableAdapter>();

        public Func<ValidationResult> ValidationCallback;

        public SelectAdapterViewModel(List<ILoadFiles> adapters, PluginManager pluginManager, string fileName)
        {
            foreach (var adapter in adapters)
                Adapters.Add(new SelectableAdapter(adapter, pluginManager));

            SelectedAdapter = Adapters.FirstOrDefault();

            RememberMySelectionText = "Remember my selection for " + Path.GetFileName(fileName);
        }

        public SelectableAdapter SelectedAdapter
        {
            get => _selectedAdapter;
            set
            {
                if (_selectedAdapter != value)
                    _selectedAdapter = value;
                NotifyOfPropertyChange(() => SelectedAdapter);
            }
        }

        public bool RememberMySelection { get; set; }

        public string RememberMySelectionText
        {
            get => _rememberMySelectionText;
            set
            {
                if (_rememberMySelectionText != value)
                    _rememberMySelectionText = value;
                NotifyOfPropertyChange(() => RememberMySelectionText);
            }
        }

        public ILoadFiles Adapter;

        public void OKButton()
        {
            Adapter = SelectedAdapter?.Adapter;

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

    public sealed class SelectableAdapter
    {
        public ILoadFiles Adapter { get; }
        public string ID { get; }
        public string Name { get; }
        public string ShortName { get; }
        public string Author { get; }
        public string About { get; }
        public string Version { get; }

        public SelectableAdapter(ILoadFiles adapter, PluginManager pluginManager)
        {
            Adapter = adapter;

            // TODO: Get metadata from plugin
            try
            {
                var attr = new PluginInfoAttribute("test"); //pluginLoader.GetMetadata<PluginInfoAttribute>(Adapter));

                ID = attr.ID;
                Name = attr.Name;
                ShortName = attr.ShortName;
                Author = attr.Author;
                About = attr.About;
                Version = FileVersionInfo.GetVersionInfo(Adapter.GetType().Assembly.Location).ProductVersion;
            }
            catch (Exception ex)
            {
                Name = Adapter.GetType().AssemblyQualifiedName;
                Version = FileVersionInfo.GetVersionInfo(Adapter.GetType().Assembly.Location).ProductVersion;
            }
        }
    }
}
