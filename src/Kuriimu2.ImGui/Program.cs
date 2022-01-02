using ImGui.Forms;
using Kuriimu2.ImGui.Forms;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui
{
    static class Program
    {
        public static void Main(string[] args)
        {
            new Application(LocalizationResources.Instance).Execute(new MainForm());
        }
    }
}
