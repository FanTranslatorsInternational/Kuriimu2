using System;
using System.Collections.Generic;
using Konnect.Contract.Enums.Plugin.File;

namespace Kuriimu2.ImGui.Models.Forms.Dialogs
{
    internal record FilePreference(string FilePath, Guid PluginId, string Name, PluginType Type, IList<string> Options);
}
