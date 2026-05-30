using System;
using System.Linq;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Plugin.File;

namespace Kuriimu2.Cmd.Processors
{
    internal class ListPluginProcessor : PluginProcessor
    {
        public void List()
        {
            var pluginManager = CreatePluginManager();
            if (pluginManager is null)
            {
                Console.WriteLine("Could not load plugins.");
                return;
            }

            var plugins = pluginManager.GetPlugins<IFilePlugin>();
            foreach (var plugin in plugins.OrderBy(x => x.PluginType).ThenBy(x => x.Metadata.Developer).ThenBy(x => x.Metadata.Name))
            {
                if (plugin.PluginType is PluginType.Font)
                    continue;

                Console.WriteLine($"{plugin.PluginId}\t{plugin.PluginType}\t{plugin.Metadata.Name}\t{plugin.Metadata.Developer}\t{plugin.Metadata.Publisher}");
            }
        }
    }
}
