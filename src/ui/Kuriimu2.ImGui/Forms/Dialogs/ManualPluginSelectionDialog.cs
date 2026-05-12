using System;
using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Plugin.File;
using Kuriimu2.ImGui.Resources;
#pragma warning disable IL3002

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class ManualPluginSelectionDialog : Modal
    {
        private readonly IList<IFilePlugin> _allPlugins;
        private readonly IList<IFilePlugin> _filteredPlugins;

        public IFilePlugin? SelectedPlugin { get; private set; }

        public ManualPluginSelectionDialog(IList<IFilePlugin> allFilePlugins, IList<IFilePlugin> filteredFilePlugins, SelectionStatus status)
        {
            InitializeComponent();

            _allPlugins = [.. allFilePlugins];
            _filteredPlugins = [.. filteredFilePlugins];

            switch (status)
            {
                case SelectionStatus.All:
                    _msgLabel.Text = LocalizationResources.DialogPluginsManualSelectionIdentification;
                    _showAllPlugins.Enabled = false;
                    _showAllPlugins.Checked = true;
                    break;

                case SelectionStatus.MultipleMatches:
                    _msgLabel.Text = LocalizationResources.DialogPluginsManualSelectionIdentificationMultiple;
                    break;

                case SelectionStatus.NonIdentifiable:
                    _msgLabel.Text = LocalizationResources.DialogPluginsManualSelectionIdentificationNone;
                    _showAllPlugins.Tooltip = LocalizationResources.DialogPluginsManualSelectionIdentificationNote;
                    break;
            }

            ListPlugins(_filteredPlugins);

            _continueButton.Clicked += ContinueButton_Clicked;
            _cancelButton.Clicked += CancelButton_Clicked;
            _showAllPlugins.CheckChanged += ShowAllPlugins_CheckChanged;
        }

        private void ListPlugins(IEnumerable<IFilePlugin> plugins)
        {
            _pluginList.Items.Clear();

            foreach (var groupedPlugins in plugins.GroupBy(x => x.GetType().Assembly))
            {
                var pluginElements = new System.Collections.Generic.List<DataTableRow<ChoosePluginElement>>();
                foreach (var plugin in groupedPlugins.OrderBy(x => x.Metadata.Name))
                    pluginElements.Add(new DataTableRow<ChoosePluginElement>(new ChoosePluginElement(plugin)));

                _pluginList.Items.Add(new Expander(CreateDataTable(pluginElements), groupedPlugins.Key.ManifestModule.Name)
                {
                    Size = Size.WidthAlign,
                    WidthIndent = 0
                });
            }
        }

        private DataTable<ChoosePluginElement> CreateDataTable(System.Collections.Generic.List<DataTableRow<ChoosePluginElement>> plugins)
        {
            var dataTable = new DataTable<ChoosePluginElement>
            {
                Columns =
                {
                    new DataTableColumn<ChoosePluginElement>(e => e.Name, LocalizationResources.DialogPluginsManualSelectionName),
                    new DataTableColumn<ChoosePluginElement>(e => LocalizationResources.DialogPluginsInstalledType(e.Type), LocalizationResources.DialogPluginsManualSelectionType),
                    new DataTableColumn<ChoosePluginElement>(e => e.Description, LocalizationResources.DialogPluginsManualSelectionDescription),
                    new DataTableColumn<ChoosePluginElement>(e => $"{e.PluginId}", LocalizationResources.DialogPluginsManualSelectionId)
                },
                Rows = plugins,
                Size = Size.WidthAlign
            };


            dataTable.SelectedRowsChanged += DataTable_SelectedRowsChanged;
            dataTable.DoubleClicked += DataTable_DoubleClicked;

            return dataTable;
        }

        #region Events

        private void ContinueButton_Clicked(object? sender, EventArgs e)
        {
            Result = DialogResult.Ok;

            Close();
        }

        private void CancelButton_Clicked(object? sender, EventArgs e)
        {
            Result = DialogResult.Cancel;

            Close();
        }

        private void ShowAllPlugins_CheckChanged(object? sender, EventArgs e)
        {
            ListPlugins(_showAllPlugins.Checked ? _allPlugins : _filteredPlugins);
        }

        private void DataTable_SelectedRowsChanged(object? sender, EventArgs e)
        {
            var dataTable = (DataTable<ChoosePluginElement>?)sender;
            if (dataTable is null || dataTable.SelectedRows.Count <= 0)
                return;

            SelectedPlugin = dataTable.SelectedRows[0].Data.Plugin;

            _continueButton.Enabled = true;
        }

        private void DataTable_DoubleClicked(object? sender, EventArgs e)
        {
            var dataTable = (DataTable<ChoosePluginElement>?)sender;
            if (dataTable is null || dataTable.SelectedRows.Count <= 0)
                return;

            SelectedPlugin = dataTable.SelectedRows[0].Data.Plugin;
            Result = DialogResult.Ok;

            Close();
        }

        #endregion
    }

    internal class ChoosePluginElement(IFilePlugin plugin)
    {
        public IFilePlugin Plugin { get; } = plugin;

        public string Name => Plugin.Metadata.Name;
        public string Description => Plugin.Metadata.LongDescription ?? "<undefined>";

        public PluginType Type => Plugin.PluginType;

        public Guid PluginId => Plugin.PluginId;
    }
}
