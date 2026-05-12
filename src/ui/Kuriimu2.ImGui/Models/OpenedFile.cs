using System.Drawing;
using ImGui.Forms.Controls;
using Konnect.Contract.Management.Files;
using Kuriimu2.ImGui.Interfaces;

namespace Kuriimu2.ImGui.Models
{
    internal class OpenedFile(IFileState state, IKuriimuForm form, TabPage page, Color color)
    {
        public IFileState FileState { get; } = state;

        public IKuriimuForm Form { get; } = form;

        public TabPage TabPage { get; } = page;

        public Color TabColor { get; } = color;
    }
}
