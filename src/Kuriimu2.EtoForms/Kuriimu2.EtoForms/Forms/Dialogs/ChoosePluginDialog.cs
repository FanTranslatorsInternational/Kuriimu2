using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Eto.Forms;
using Kontract.Interfaces.Plugins.Identifier;
using Kore.Models.UnsupportedPlugin;
using Kuriimu2.EtoForms.Forms.Models;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class ChoosePluginDialog : Dialog<IFilePlugin>
    {
        private readonly string _message;
        private readonly string _filterNote;
        private readonly IReadOnlyList<IFilePlugin> _filteredPlugins;
        private readonly IReadOnlyList<IFilePlugin> _allPlugins;

        private IFilePlugin _selectedFilePlugin;

        public ChoosePluginDialog(string message, IReadOnlyList<IFilePlugin> filePlugins, string filterNote, IReadOnlyList<IFilePlugin> filteredPlugins)
        {
            _message = message;
            _allPlugins = filePlugins;
            _filterNote = filterNote;
            _filteredPlugins = filteredPlugins;
            
            InitializeComponent();
            ListPlugins(filteredPlugins ?? filePlugins);

            okButtonCommand.Executed += OkButtonCommand_Executed;
            viewRawButtonCommand.Executed += ViewRawButtonCommandExecuted;
            cancelButtonCommand.Executed += CancelButtonCommand_Executed;
            showAllCheckbox.CheckedChanged += ShowAllCheckbox_Changed;
        }

        private void ListPlugins(IReadOnlyList<IFilePlugin> plugins)
        {
            pluginListPanel.Items.Clear();
            
            foreach (var groupedPlugins in plugins.GroupBy(x => x.GetType().Assembly))
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
                        HeaderText = "Description",
                        DataCell = new TextBoxCell{Binding = Binding.Property<ChoosePluginElement,string>(p=>p.Description)}
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

        private void OkButtonCommand_Executed(object sender, EventArgs e)
        {
            Close(_selectedFilePlugin);
        }

        private void ViewRawButtonCommandExecuted(object sender, EventArgs e)
        {
            Close(new HexPlugin());
        }

        private void CancelButtonCommand_Executed(object sender, EventArgs e)
        {
            Close(null);
        }

        private void ShowAllCheckbox_Changed(object sender, EventArgs e)
        {
            ListPlugins(showAllCheckbox.Checked.Value ? _allPlugins : _filteredPlugins);
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
