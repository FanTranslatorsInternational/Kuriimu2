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

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class InstalledPluginsDialog : Modal
    {
        private void InitializeComponent(IPluginManager pluginManager)
        {
            var typeList = new global::ImGui.Forms.Controls.Lists.List<Expander>
            {
                ItemSpacing = 4
            };

            IEnumerable<IFilePlugin> filePlugins = pluginManager.GetPlugins<IFilePlugin>();
            foreach (var typeGroup in filePlugins.GroupBy(x => x.PluginType))
            {
                var typePluginTable = new DataTable<InstalledPlugin>
                {
                    Size = Size.WidthAlign,
                    Columns =
                    {
                        new DataTableColumn<InstalledPlugin>(metadata => $"{metadata.PluginId}", LocalizationResources.DialogPluginsInstalledId),
                        new DataTableColumn<InstalledPlugin>(metadata => metadata.Metadata.Name, LocalizationResources.DialogPluginsInstalledName){SortOrder = 2},
                        new DataTableColumn<InstalledPlugin>(metadata => metadata.Metadata.Publisher ?? string.Empty, LocalizationResources.DialogPluginsInstalledPublisher){SortOrder = 0},
                        new DataTableColumn<InstalledPlugin>(metadata => metadata.Metadata.Developer, LocalizationResources.DialogPluginsInstalledDeveloper){SortOrder = 1},
                        new DataTableColumn<InstalledPlugin>(metadata => string.Join(',', metadata.Metadata.Author.Order()), LocalizationResources.DialogPluginsInstalledAuthors),
                        new DataTableColumn<InstalledPlugin>(metadata => string.Join(',', metadata.Metadata.Platform.Order()), LocalizationResources.DialogPluginsInstalledPlatforms)
                    }
                };

                foreach (var plugin in typeGroup)
                    typePluginTable.Rows.Add(new DataTableRow<InstalledPlugin>(new InstalledPlugin(plugin.PluginId, plugin.Metadata)));

                typeList.Items.Add(new Expander(typePluginTable, LocalizationResources.DialogPluginsInstalledType(typeGroup.Key))
                {
                    Size = Size.WidthAlign,
                    Expanded = true,
                    WidthIndent = 0
                });
            }

            Caption = LocalizationResources.DialogPluginsInstalledCaption;

            Content = typeList;
            Size = new Size(SizeValue.Relative(.7f), SizeValue.Relative(.8f));
        }
    }
}
