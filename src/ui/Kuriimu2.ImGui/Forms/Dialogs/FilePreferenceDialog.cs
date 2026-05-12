using Konnect.Contract.Management.Files;
using Konnect.Contract.Management.Plugin;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class FilePreferenceDialog
    {
        private readonly IFilePreferences _preferences;

        public FilePreferenceDialog(IFilePreferences preferences, IPluginManager pluginManager)
        {
            _preferences = preferences;

            InitializeComponent(preferences, pluginManager);
        }

        private void RemoveSelectedRows()
        {
            if (_preferenceTable.SelectedRows.Count <= 0)
                return;

            var rows = _preferenceTable.Rows;

            foreach (var selectedRow in _preferenceTable.SelectedRows)
            {
                _preferences.Remove(selectedRow.Data.FilePath);
                rows.Remove(selectedRow);
            }

            _preferenceTable.Rows = rows;
        }
    }
}
