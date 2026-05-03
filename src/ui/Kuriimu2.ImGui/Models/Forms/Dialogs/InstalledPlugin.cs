using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Konnect.Contract.DataClasses.Plugin;

namespace Kuriimu2.ImGui.Models.Forms.Dialogs
{
    internal record InstalledPlugin(Guid PluginId, PluginMetadata Metadata);
}
