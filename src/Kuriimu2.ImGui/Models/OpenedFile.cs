using System.Drawing;
using ImGui.Forms.Controls;
using Kontract.Interfaces.Managers;
using Kuriimu2.ImGui.Interfaces;

namespace Kuriimu2.ImGui.Models
{
    class OpenedFile
    {
        public IFileState FileState { get; }

        public IKuriimuForm Form { get; }

        public TabPage TabPage { get; }

        public Color TabColor { get; }

        public OpenedFile(IFileState state, IKuriimuForm form, TabPage page, Color color)
        {
            FileState = state;
            Form = form;
            TabPage = page;
            TabColor = color;
        }
    }
}
