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
using Kuriimu2.ImGui.Models;
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
                    _msgLabel.Text = LocalizationResources.DialogChoosePluginHeaderGeneric;
                    _showAllPlugins.Enabled = false;
                    _showAllPlugins.Checked = true;
                    break;

                case SelectionStatus.MultipleMatches:
                    _msgLabel.Text = LocalizationResources.DialogChoosePluginHeaderIdentificationMultiple;
                    break;

                case SelectionStatus.NonIdentifiable:
                    _msgLabel.Text = LocalizationResources.DialogChoosePluginHeaderIdentificationNone;
                    _showAllPlugins.Tooltip = LocalizationResources.DialogChoosePluginHeaderIdentificationNote;
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
                var pluginElements = new System.Collections.Generic.List<DataTableRow<ChoosePluginElement>>();
                foreach (var plugin in groupedPlugins.OrderBy(x => x.Metadata?.Name ?? string.Empty))
                    pluginElements.Add(new DataTableRow<ChoosePluginElement>(new ChoosePluginElement(plugin)));

                _pluginList.Items.Add(new Expander(CreateDataTable(pluginElements), groupedPlugins.Key.ManifestModule.Name)
                {
                    Size = Size.WidthAlign,
                    WidthIndent = 0
                });
            }
        }

        private DataTable<ChoosePluginElement> CreateDataTable(IList<DataTableRow<ChoosePluginElement>> plugins)
        {
            var dataTable = new DataTable<ChoosePluginElement>
            {
                Columns =
                {
                    new DataTableColumn<ChoosePluginElement>(e => e.Name, LocalizationResources.DialogChoosePluginPluginsTableName),
                    new DataTableColumn<ChoosePluginElement>(e => LocalizationResources.MenuPluginsType(e.Type), LocalizationResources.DialogChoosePluginPluginsTableType),
                    new DataTableColumn<ChoosePluginElement>(e => e.Description, LocalizationResources.DialogChoosePluginPluginsTableDescription),
                    new DataTableColumn<ChoosePluginElement>(e => e.PluginId.ToString("N"), LocalizationResources.DialogChoosePluginPluginsTableId)
                },
                Rows = plugins,
                Size = Size.WidthAlign
            };


            dataTable.SelectedRowsChanged += DataTable_SelectedRowsChanged;
            dataTable.DoubleClicked += DataTable_DoubleClicked;

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

            SelectedPlugin = dataTable.SelectedRows.First().Data.Plugin;
            _continueButton.Enabled = true;
        }

        private void DataTable_DoubleClicked(object sender, EventArgs e)
        {
            var dataTable = (DataTable<ChoosePluginElement>)sender;
            if (!dataTable.SelectedRows.Any()) return;

            SelectedPlugin = dataTable.SelectedRows.First().Data.Plugin;
            Result = DialogResult.Ok;

            Close();
        }

        #endregion
    }

    class ChoosePluginElement
    {
        public IFilePlugin Plugin { get; }

        public string Name => Plugin.Metadata?.Name ?? "<undefined>";
        public string Description => Plugin.Metadata?.LongDescription ?? "<undefined>";

        public PluginType Type => Plugin.PluginType;

        public Guid PluginId => Plugin.PluginId;

        public ChoosePluginElement(IFilePlugin plugin)
        {
            Plugin = plugin;
        }
    }
}
