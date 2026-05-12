using System;

namespace Kuriimu2.ImGui.Models.Forms.Dialogs
{
    [Flags]
    internal enum SelectablePlugins : byte
    {
        Text = 1,
        Image = 2,
        Archive = 4,
        Font = 8,
        All = 15,
    }
}
