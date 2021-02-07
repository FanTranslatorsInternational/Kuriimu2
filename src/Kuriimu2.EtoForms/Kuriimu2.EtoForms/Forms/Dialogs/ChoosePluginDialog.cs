using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Eto.Forms;
using Kontract.Interfaces.Plugins.Identifier;
using Kuriimu2.EtoForms.Forms.Models;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class ChoosePluginDialog : Dialog<IFilePlugin>
    {
        private readonly IReadOnlyList<IFilePlugin> _filePlugins;

        private IFilePlugin _selectedFilePlugin;

        public ChoosePluginDialog(IReadOnlyList<IFilePlugin> filePlugins)
        {
            InitializeComponent();

            _filePlugins = filePlugins;
            AddPlugins();

            okButtonCommand.Executed += OkButtonCommand_Executed;
            cancelButtonCommand.Executed += CancelButtonCommand_Executed;
        }

        private void AddPlugins()
        {
            foreach (var groupedPlugins in _filePlugins.GroupBy(x => x.GetType().Assembly))
            {
                var pluginStore = new ObservableCollection<object>();
                foreach (var plugin in groupedPlugins.OrderBy(x => x.Metadata?.Name ?? "<undefined>"))
                    pluginStore.Add(new ChoosePluginElement(plugin));

                pluginListPanel.Items.Add(new Expander
                {
                    Header = new Label
                    {
                        Text = groupedPlugins.Key.ManifestModule.Name
                    },
                    Content = CreateGridView(pluginStore)
                });
            }

            Invalidate();
        }

        private GridView CreateGridView(IEnumerable<object> dataStore)
        {
            var gridView = new GridView
            {
                Border = BorderType.None,
                DataStore = dataStore,
                Columns =
                {
                    new GridColumn
                    {
                        HeaderText = "Name",
                        DataCell = new TextBoxCell{Binding = Binding.Property<ChoosePluginElement,string>(p=>p.Name)},
                        Sortable = true,
                        AutoSize = true
                    },
                    new GridColumn
                    {
                        HeaderText = "Type",
                        DataCell = new TextBoxCell{Binding = Binding.Property<ChoosePluginElement,string>(p=>p.Type.ToString())},
                        Sortable = true,
                        AutoSize = true
                    },
                    new GridColumn
                    {
                        HeaderText = "GUID",
                        DataCell = new TextBoxCell{Binding = Binding.Property<ChoosePluginElement,string>(p=>p.PluginId.ToString("D"))},
                        Sortable = true,
                        AutoSize = true
                    }
                }
            };

            gridView.SelectedRowsChanged += GridView_SelectedRowsChanged;
            gridView.CellDoubleClick += GridView_CellDoubleClick;
            return gridView;
        }

        #region Events

        protected override void OnShown(EventArgs e)
        {
            _selectedFilePlugin = null;
            okButton.Enabled = false;
        }

        private void CancelButtonCommand_Executed(object sender, EventArgs e)
        {
            Close(null);
        }

        private void OkButtonCommand_Executed(object sender, EventArgs e)
        {
            Close(_selectedFilePlugin);
        }

        private void GridView_SelectedRowsChanged(object sender, EventArgs e)
        {
            var gridView = (GridView)sender;
            if (gridView.SelectedItem == null) return;

            _selectedFilePlugin = ((ChoosePluginElement)gridView.SelectedItem).Plugin;
            okButton.Enabled = true;
        }

        private void GridView_CellDoubleClick(object sender, GridCellMouseEventArgs e)
        {
            var gridView = (GridView)sender;
            if (gridView.SelectedItem == null) return;

            Close(((ChoosePluginElement)gridView.SelectedItem).Plugin);
        }

        #endregion
    }
}
