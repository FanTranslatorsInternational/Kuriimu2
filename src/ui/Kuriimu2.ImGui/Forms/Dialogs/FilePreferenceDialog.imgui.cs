using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Support;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Kuriimu2.ImGui.Models.Forms.Dialogs;
using Kuriimu2.ImGui.Resources;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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

        [MemberNotNull(nameof(_preferenceTable))]
        private void InitializeComponent(IFilePreferences preferences, IPluginManager pluginManager)
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
        }

        private void InitializePreferences(IFilePreferences preferences, IPluginManager pluginManager)
        {
            var files = preferences.GetPaths();

            foreach (var file in files)
            {
                var entry = preferences.GetOrDefault(file);
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
