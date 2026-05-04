using System.Linq;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Konnect.Management.Files;
using Kuriimu2.ImGui.Models.Forms.Dialogs;
using Kuriimu2.ImGui.Resources;
using Hexa.NET.ImGui;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Support;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class FilePreferenceDialog : Modal
    {
        private static readonly KeyCommand Delete = new(ImGuiKey.Delete);

        private DataTable<FilePreference> _preferenceTable;

        protected override void UpdateInternal(Rectangle contentRect)
        {
            base.UpdateInternal(contentRect);

            if (Delete.IsPressed())
                RemoveSelectedRows();
        }

        private void InitializeComponent(IPluginManager pluginManager)
        {
            _preferenceTable = new DataTable<FilePreference>
            {
                Size = Size.Parent,
                Columns =
                {
                    new DataTableColumn<FilePreference>(value => value.FilePath, LocalizationResources.DialogPreferencesPath),
                    new DataTableColumn<FilePreference>(value => $"{value.PluginId}", LocalizationResources.DialogPreferencesId){SortOrder = 1},
                    new DataTableColumn<FilePreference>(value => $"{value.Name}", LocalizationResources.DialogPreferencesName),
                    new DataTableColumn<FilePreference>(value => LocalizationResources.DialogPreferencesType(value.Type), LocalizationResources.DialogPreferencesTypeCaption){SortOrder = 0},
                    new DataTableColumn<FilePreference>(value => string.Join(';', value.Options.Select(o => o)), LocalizationResources.DialogPreferencesOptions)
                }
            };

            Caption = LocalizationResources.DialogPreferencesCaption;

            Content = _preferenceTable;
            Size = new Size(SizeValue.Relative(.7f), SizeValue.Relative(.8f));

            InitializePreferences(pluginManager);
        }

        private void InitializePreferences(IPluginManager pluginManager)
        {
            var files = FilePreferences.GetPaths();

            foreach (var file in files)
            {
                var entry = FilePreferences.GetOrDefault(file);
                if (entry is null)
                    continue;

                var plugin = pluginManager.GetPlugin<IFilePlugin>(entry.Value.PluginId);
                if (plugin is null)
                    continue;

                var preference = new FilePreference(file, entry.Value.PluginId, plugin.Metadata.Name, plugin.PluginType, entry.Value.Options);

                _preferenceTable.Rows.Add(new DataTableRow<FilePreference>(preference));
            }
        }
    }
}
