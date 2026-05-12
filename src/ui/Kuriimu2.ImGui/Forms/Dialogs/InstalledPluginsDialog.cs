using Konnect.Contract.Management.Plugin;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class InstalledPluginsDialog
    {
        public InstalledPluginsDialog(IPluginManager pluginManager)
        {
            InitializeComponent(pluginManager);
        }
    }
}
