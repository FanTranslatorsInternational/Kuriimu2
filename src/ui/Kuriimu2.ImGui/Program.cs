using ImGui.Forms;
using ImGui.Forms.Resources;
using Kuriimu2.ImGui.Forms;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui
{
    static class Program
    {
        public static void Main(string[] args)
        {
            var app = new Application(LocalizationResources.Instance);

            FontResources.RegisterArial(15);

            app.Execute(new MainForm());
        }
    }
}
