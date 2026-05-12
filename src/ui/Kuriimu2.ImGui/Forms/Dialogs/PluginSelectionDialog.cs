using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Modals;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Kuriimu2.ImGui.Models.Forms.Dialogs;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class PluginSelectionDialog
    {
        public IFilePlugin? SelectedPlugin { get; private set; }

        public PluginSelectionDialog(IPluginManager pluginManager, SelectablePlugins selectable = SelectablePlugins.All)
        {
            InitializeComponent(pluginManager, selectable);
        }

        private void TypePluginTable_DoubleClicked(object? sender, System.EventArgs e)
        {
            var table = (DataTable<IFilePlugin>?)sender;
            if (table is null || table.SelectedRows.Count <= 0)
                return;

            SelectedPlugin = table.SelectedRows[0].Data;

            Close(DialogResult.Ok);
        }
    }
}
