using System.Drawing;
using System.Reflection;

namespace Kuriimu2.ImGui.Resources
{
    static class ImageResources
    {
        #region Resource Names

        private const string IconResource_ = "Kuriimu2.ImGui.Resources.Images.kuriimu2.ico";

        #endregion

        #region Resource Instances

        public static Image Icon => FromResource(IconResource_);

        #endregion

        private static Image FromResource(string name)
        {
            var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            return resourceStream == null ? null : Image.FromStream(resourceStream);
        }
    }
}
