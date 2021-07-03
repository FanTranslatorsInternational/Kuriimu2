using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Eto.Forms;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kore.Models.UnsupportedPlugin;
using Kuriimu2.EtoForms.Forms.Models;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    // TODO: For SelectionStatus.All, we may provide a ctor that only takes a list of all file plugins instead to reduce external overhead
    partial class ChoosePluginDialog : Dialog<IFilePlugin>
    {
        private readonly string _message;
        private readonly string _filterNote;

        private readonly IList<IFilePlugin> _allPlugins;
        private readonly IList<IFilePlugin> _filteredPlugins;

        private IFilePlugin _selectedFilePlugin;

        #region Localizuation Keys

        private const string ChooseOpenFilePluginKey_ = "ChooseOpenFilePlugin";
        private const string MultiplePluginMatchesSelectionKey_ = "MultiplePluginMatchesSelection";
        private const string NonIdentifiablePluginSelectionKey_ = "NonIdentifiablePluginSelection";
        private const string NonIdentifiablePluginSelectionNoteKey_ = "NonIdentifiablePluginSelectionNote";

        private const string PluginNameColumnKey_ = "PluginNameColumn";
        private const string PluginTypeColumnKey_ = "PluginTypeColumn";
        private const string PluginDescriptionColumnKey_ = "PluginDescriptionColumn";
        private const string PluginIdColumnKey_ = "PluginIdColumn";

        #endregion

        public ChoosePluginDialog(IList<IFilePlugin> allFilePlugins, IList<IFilePlugin> filteredFilePlugins, SelectionStatus status)
        {
            _allPlugins = allFilePlugins.ToArray();
            _filteredPlugins = filteredFilePlugins.ToArray();

            switch (status)
            {
                case SelectionStatus.All:
                    _message = Localize(ChooseOpenFilePluginKey_);
                    showAllCheckbox.Enabled = false;
                    showAllCheckbox.Checked = true;
                    break;

                case SelectionStatus.MultipleMatches:
                    _message = Localize(MultiplePluginMatchesSelectionKey_);
                    break;

                case SelectionStatus.NonIdentifiable:
                    _message = Localize(NonIdentifiablePluginSelectionKey_);
                    _filterNote = Localize(NonIdentifiablePluginSelectionNoteKey_);
                    break;
            }

            InitializeComponent();
            ListPlugins(_filteredPlugins);

            continueButtonCommand.Executed += ContinueButtonCommandExecuted;
            viewRawButtonCommand.Executed += ViewRawButtonCommandExecuted;
            cancelButtonCommand.Executed += CancelButtonCommand_Executed;
            showAllCheckbox.CheckedChanged += ShowAllCheckbox_Changed;
        }

        private void ListPlugins(IEnumerable<IFilePlugin> plugins)
        {
            pluginListPanel.Items.Clear();

            foreach (var groupedPlugins in plugins.GroupBy(x => x.GetType().Assembly))
            {
                var pluginStore = new ObservableCollection<object>();
                foreach (var plugin in groupedPlugins.OrderBy(x => x.Metadata?.Name ?? string.Empty))
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
                        HeaderText = Localize(PluginNameColumnKey_),
                        DataCell = new TextBoxCell{Binding = Binding.Property<ChoosePluginElement,string>(p=>p.Name)},
                        Sortable = true,
                        AutoSize = true
                    },
                    new GridColumn
                    {
                        HeaderText = Localize(PluginTypeColumnKey_),
                        DataCell = new TextBoxCell{Binding = Binding.Property<ChoosePluginElement,string>(p=>p.Type.ToString())},
                        Sortable = true,
                        AutoSize = true
                    },
                    new GridColumn
                    {
                        HeaderText = Localize(PluginDescriptionColumnKey_),
                        DataCell = new TextBoxCell{Binding = Binding.Property<ChoosePluginElement,string>(p=>p.Description)}
                    },
                    new GridColumn
                    {
                        HeaderText = Localize(PluginIdColumnKey_),
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
            continueButton.Enabled = false;
        }

        private void ContinueButtonCommandExecuted(object sender, EventArgs e)
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
            continueButton.Enabled = true;
        }

        private void GridView_CellDoubleClick(object sender, GridCellMouseEventArgs e)
        {
            var gridView = (GridView)sender;
            if (gridView.SelectedItem == null) return;

            Close(((ChoosePluginElement)gridView.SelectedItem).Plugin);
        }

        #endregion

        private string Localize(string name, params object[] args)
        {
            return string.Format(Application.Instance.Localize(this, name), args);
        }
    }
}
