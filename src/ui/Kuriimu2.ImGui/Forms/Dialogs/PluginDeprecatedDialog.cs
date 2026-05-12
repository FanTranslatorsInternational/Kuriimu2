using Konnect.Contract.Plugin.File;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class PluginDeprecatedDialog
    {
        public PluginDeprecatedDialog(IDeprecatedFilePlugin deprecatedPlugin)
        {
            InitializeComponent(deprecatedPlugin);
        }
    }
}
