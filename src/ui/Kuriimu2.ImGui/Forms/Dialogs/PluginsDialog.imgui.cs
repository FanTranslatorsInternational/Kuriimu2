using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class PluginsDialog : Modal
    {
        private void InitializeComponents(IPluginManager pluginManager)
        {
            var typeList = new global::ImGui.Forms.Controls.Lists.List<Expander>
            {
                ItemSpacing = 4
            };

            IEnumerable<IFilePlugin> filePlugins = pluginManager.GetPlugins<IFilePlugin>();
            foreach (var typeGroup in filePlugins.GroupBy(x => x.PluginType))
            {
                var typePluginTable = new DataTable<PluginMetadata>
                {
                    IsSelectable = true,
                    IsResizable = false,
                    CanSelectMultiple = true,
                    ShowHeaders = true,
                    Size = Size.WidthAlign,
                    Columns =
                    {
                        new DataTableColumn<PluginMetadata>(metadata => metadata.Name, LocalizationResources.MenuPluginsName),
                        new DataTableColumn<PluginMetadata>(metadata => metadata.Publisher ?? string.Empty, LocalizationResources.MenuPluginsPublisher),
                        new DataTableColumn<PluginMetadata>(metadata => metadata.Developer, LocalizationResources.MenuPluginsDeveloper),
                        new DataTableColumn<PluginMetadata>(metadata => string.Join(',', metadata.Author.Order()), LocalizationResources.MenuPluginsAuthors),
                        new DataTableColumn<PluginMetadata>(metadata => string.Join(',', metadata.Platform.Order()), LocalizationResources.MenuPluginsPlatforms)
                    }
                };

                foreach (var plugin in typeGroup.OrderBy(x => x.Metadata.Publisher).ThenBy(x => x.Metadata.Developer).ThenBy(x => x.Metadata.Name))
                    typePluginTable.Rows.Add(new DataTableRow<PluginMetadata>(plugin.Metadata));

                typeList.Items.Add(new Expander(typePluginTable, LocalizationResources.MenuPluginsType(typeGroup.Key))
                {
                    Size = Size.WidthAlign,
                    Expanded = true,
                    WidthIndent = 0
                });
            }

            Caption = LocalizationResources.MenuPluginsTitle;

            Content = typeList;
            Size = new Size(SizeValue.Relative(.7f), SizeValue.Relative(.8f));
        }
    }
}
