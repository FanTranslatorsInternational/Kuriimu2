using Konnect.Contract.Management.Plugin;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class PluginsDialog
    {
        public PluginsDialog(IPluginManager pluginManager)
        {
            InitializeComponents(pluginManager);
        }
    }
}
