using System;
using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Modals;
using Kontract;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Models;
using Kore.Models.UnsupportedPlugin;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class ChoosePluginDialog : Modal
    {
        private readonly IList<IFilePlugin> _allPlugins;
        private readonly IList<IFilePlugin> _filteredPlugins;

        public IFilePlugin SelectedPlugin { get; private set; }

        public ChoosePluginDialog(IList<IFilePlugin> allFilePlugins, IList<IFilePlugin> filteredFilePlugins, SelectionStatus status)
        {
            InitializeComponent();

            _allPlugins = allFilePlugins.ToArray();
            _filteredPlugins = filteredFilePlugins.ToArray();

            switch (status)
            {
                case SelectionStatus.All:
                    _msgLabel.Caption = LocalizationResources.ChooseOpenFilePluginResource();
                    _showAllPlugins.Enabled = false;
                    _showAllPlugins.Checked = true;
                    break;

                case SelectionStatus.MultipleMatches:
                    _msgLabel.Caption = LocalizationResources.MultiplePluginMatchesSelectionResource();
                    break;

                case SelectionStatus.NonIdentifiable:
                    _msgLabel.Caption = LocalizationResources.NonIdentifiablePluginSelectionResource();
                    _showAllPlugins.Tooltip = LocalizationResources.NonIdentifiablePluginSelectionNoteResource();
                    break;
            }

            ListPlugins(_filteredPlugins);

            _continueButton.Clicked += _continueButton_Clicked;
            _viewRawButton.Clicked += _viewRawButton_Clicked;
            _cancelButton.Clicked += _cancelButton_Clicked;
            _showAllPlugins.CheckChanged += _showAllPlugins_CheckChanged;
        }

        private void ListPlugins(IEnumerable<IFilePlugin> plugins)
        {
            _pluginList.Items.Clear();

            foreach (var groupedPlugins in plugins.GroupBy(x => x.GetType().Assembly))
            {
                var pluginElements = new List<ChoosePluginElement>();
                foreach (var plugin in groupedPlugins.OrderBy(x => x.Metadata?.Name ?? string.Empty))
                    pluginElements.Add(new ChoosePluginElement(plugin));

                _pluginList.Items.Add(new Expander
                {
                    Caption = groupedPlugins.Key.ManifestModule.Name,
                    Content = CreateDataTable(pluginElements)
                });
            }
        }

        private DataTable<ChoosePluginElement> CreateDataTable(IList<ChoosePluginElement> plugins)
        {
            var dataTable = new DataTable<ChoosePluginElement>
            {
                Columns =
                {
                    new DataTableColumn<ChoosePluginElement>(e => e.Name, LocalizationResources.PluginNameColumnResource()),
                    new DataTableColumn<ChoosePluginElement>(e => e.Type.ToString(), LocalizationResources.PluginTypeColumnResource()),
                    new DataTableColumn<ChoosePluginElement>(e => e.Description, LocalizationResources.PluginDescriptionColumnResource()),
                    new DataTableColumn<ChoosePluginElement>(e => e.PluginId.ToString("N"), LocalizationResources.PluginIdColumnResource())
                },
                Rows = plugins
            };


            dataTable.SelectedRowsChanged += DataTable_SelectedRowsChanged;
            dataTable.DoubleClicked += DataTable_DoubleClicked; ;

            return dataTable;
        }

        #region Events

        private void _continueButton_Clicked(object sender, EventArgs e)
        {
            Result = DialogResult.Ok;

            Close();
        }

        private void _viewRawButton_Clicked(object sender, EventArgs e)
        {
            SelectedPlugin = new HexPlugin();
            Result = DialogResult.Ok;

            Close();
        }

        private void _cancelButton_Clicked(object sender, EventArgs e)
        {
            Result = DialogResult.Cancel;

            Close();
        }

        private void _showAllPlugins_CheckChanged(object sender, EventArgs e)
        {
            ListPlugins(_showAllPlugins.Checked ? _allPlugins : _filteredPlugins);
        }

        private void DataTable_SelectedRowsChanged(object sender, EventArgs e)
        {
            var dataTable = (DataTable<ChoosePluginElement>)sender;
            if (!dataTable.SelectedRows.Any()) return;

            SelectedPlugin = dataTable.SelectedRows.First().Plugin;
            _continueButton.Enabled = true;
        }

        private void DataTable_DoubleClicked(object sender, EventArgs e)
        {
            var dataTable = (DataTable<ChoosePluginElement>)sender;
            if (!dataTable.SelectedRows.Any()) return;

            SelectedPlugin = dataTable.SelectedRows.First().Plugin;
            Result = DialogResult.Ok;

            Close();
        }

        #endregion
    }

    class ChoosePluginElement
    {
        public IFilePlugin Plugin { get; }

        public string Name => Plugin.Metadata?.Name ?? "<undefined>";
        public string Description => Plugin.Metadata?.ShortDescription ?? "<undefined>";

        public PluginType Type => Plugin.PluginType;

        public Guid PluginId => Plugin.PluginId;

        public ChoosePluginElement(IFilePlugin plugin)
        {
            ContractAssertions.IsNotNull(plugin, nameof(plugin));
            Plugin = plugin;
        }
    }
}
