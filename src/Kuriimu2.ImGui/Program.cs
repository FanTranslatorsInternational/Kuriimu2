using ImGui.Forms;
using Kuriimu2.ImGui.Forms;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui
{
    static class Program
    {
        public static void Main(string[] args)
        {
            Application.Create(new MainForm(), LocalizationResources.Instance).Execute();
        }
    }
}
