using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Caliburn.Micro;
using Kontract.Interfaces.Plugins.Identifier;
using Kore.Extensions;
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

        public SelectAdapterViewModel(List<IFilePlugin> adapters, FileManager pluginManager, string fileName)
        {
            foreach (var adapter in adapters)
                Adapters.Add(new SelectableAdapter(adapter.PluginId, pluginManager));

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

        public IFilePlugin Adapter;

        public void OKButton()
        {
            Adapter = SelectedAdapter?.FilePlugin;

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
        public IFilePlugin FilePlugin { get; }
        public string ID { get; }
        public string Name { get; }
        public string ShortName { get; }
        public string Author { get; }
        public string About { get; }
        public string Version { get; }

        public SelectableAdapter(Guid pluginId, FileManager pluginManager)
        {
            FilePlugin = pluginManager.GetFilePlugins().FirstOrDefault(x => x.PluginId == pluginId);

            // TODO: Get metadata from plugin
            try
            {
                ID = pluginId.ToString();
                Name = FilePlugin.Metadata.Name;
                ShortName = FilePlugin.Metadata.ShortDescription;
                Author = FilePlugin.Metadata.Author;
                About = FilePlugin.Metadata.LongDescription;
                Version = FileVersionInfo.GetVersionInfo(FilePlugin.GetType().Assembly.Location).ProductVersion;
            }
            catch (Exception ex)
            {
                Name = FilePlugin.GetType().AssemblyQualifiedName;
                Version = FileVersionInfo.GetVersionInfo(FilePlugin.GetType().Assembly.Location).ProductVersion;
            }
        }
    }
}
