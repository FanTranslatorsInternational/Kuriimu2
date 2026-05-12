using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Kuriimu2.ImGui.Models.Forms.Dialogs;
using Kuriimu2.ImGui.Resources;
using System.Collections.Generic;
using System.Linq;
using Konnect.Contract.Enums.Plugin.File;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class PluginSelectionDialog : Modal
    {
        private void InitializeComponent(IPluginManager pluginManager, SelectablePlugins selectable)
        {
            var typeList = new global::ImGui.Forms.Controls.Lists.List<Expander>
            {
                ItemSpacing = 4
            };

            IEnumerable<IFilePlugin> filePlugins = pluginManager.GetPlugins<IFilePlugin>();
            foreach (var typeGroup in filePlugins.GroupBy(x => x.PluginType))
            {
                if (!IsSelectable(typeGroup.Key, selectable))
                    continue;

                var typePluginTable = new DataTable<IFilePlugin>
                {
                    Size = Size.WidthAlign,
                    CanSelectMultiple = false,
                    Columns =
                    {
                        new DataTableColumn<IFilePlugin>(metadata => $"{metadata.PluginId}", LocalizationResources.DialogPluginsSelectionId),
                        new DataTableColumn<IFilePlugin>(metadata => metadata.Metadata.Name, LocalizationResources.DialogPluginsSelectionName){SortOrder = 2},
                        new DataTableColumn<IFilePlugin>(metadata => metadata.Metadata.Publisher ?? string.Empty, LocalizationResources.DialogPluginsSelectionPublisher){SortOrder = 0},
                        new DataTableColumn<IFilePlugin>(metadata => metadata.Metadata.Developer, LocalizationResources.DialogPluginsSelectionDeveloper){SortOrder = 1},
                        new DataTableColumn<IFilePlugin>(metadata => string.Join(',', metadata.Metadata.Author.Order()), LocalizationResources.DialogPluginsSelectionAuthors),
                        new DataTableColumn<IFilePlugin>(metadata => string.Join(',', metadata.Metadata.Platform.Order()), LocalizationResources.DialogPluginsSelectionPlatforms)
                    }
                };

                foreach (var plugin in typeGroup)
                    typePluginTable.Rows.Add(new DataTableRow<IFilePlugin>(plugin));

                typeList.Items.Add(new Expander(typePluginTable, LocalizationResources.DialogPluginsSelectionType(typeGroup.Key))
                {
                    Size = Size.WidthAlign,
                    Expanded = true,
                    WidthIndent = 0
                });

                typePluginTable.DoubleClicked += TypePluginTable_DoubleClicked;
            }

            Caption = LocalizationResources.DialogPluginsSelectionCaption;

            Content = typeList;
            Size = new Size(SizeValue.Relative(.7f), SizeValue.Relative(.8f));

            Result = DialogResult.Cancel;
        }

        private static bool IsSelectable(PluginType type, SelectablePlugins selectable)
        {
            switch (type)
            {
                case PluginType.Text when selectable.HasFlag(SelectablePlugins.Text):
                case PluginType.Image when selectable.HasFlag(SelectablePlugins.Image):
                case PluginType.Archive when selectable.HasFlag(SelectablePlugins.Archive):
                case PluginType.Font when selectable.HasFlag(SelectablePlugins.Font):
                    return true;

                default:
                    return false;
            }
        }
    }
}
