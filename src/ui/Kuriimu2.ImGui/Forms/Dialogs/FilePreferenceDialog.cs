using Konnect.Contract.Management.Plugin;
using Konnect.Management.Files;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class FilePreferenceDialog
    {
        public FilePreferenceDialog(IPluginManager pluginManager)
        {
            InitializeComponent(pluginManager);
        }

        private void RemoveSelectedRows()
        {
            if (_preferenceTable.SelectedRows.Count <= 0)
                return;

            var rows = _preferenceTable.Rows;

            foreach (var selectedRow in _preferenceTable.SelectedRows)
            {
                FilePreferences.Remove(selectedRow.Data.FilePath);
                rows.Remove(selectedRow);
            }

            _preferenceTable.Rows = rows;
        }
    }
}
