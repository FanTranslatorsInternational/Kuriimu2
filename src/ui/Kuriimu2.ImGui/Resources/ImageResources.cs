using System.Drawing;
using System.Reflection;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;

namespace Kuriimu2.ImGui.Resources
{
    static class ImageResources
    {
        #region Resource Names

        private const string IconResource_ = "Kuriimu2.ImGui.Resources.Images.kuriimu2.ico";
        private const string CloseResource_ = "Kuriimu2.ImGui.Resources.Images.close.png";

        private const string SaveDarkResource_ = "Kuriimu2.ImGui.Resources.Images.dark.save.png";
        private const string SaveAsDarkResource_ = "Kuriimu2.ImGui.Resources.Images.dark.save_as.png";

        private const string SaveLightResource_ = "Kuriimu2.ImGui.Resources.Images.light.save.png";
        private const string SaveAsLightResource_ = "Kuriimu2.ImGui.Resources.Images.light.save_as.png";

        #endregion

        #region Resource Instances

        public static Image Icon => FromResource(IconResource_);

        public static ImageResource Close => ImageResource.FromResource(Assembly.GetExecutingAssembly(), CloseResource_);

        public static ImageResource Save(Theme theme) => ImageResource.FromResource(Assembly.GetExecutingAssembly(), theme == Theme.Dark ? SaveDarkResource_ : SaveLightResource_);

        public static ImageResource SaveAs(Theme theme) => ImageResource.FromResource(Assembly.GetExecutingAssembly(), theme == Theme.Dark ? SaveAsDarkResource_ : SaveAsLightResource_);

        #endregion

        #region Support

        private static Image FromResource(string name)
        {
            var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            return resourceStream == null ? null : Image.FromStream(resourceStream);
        }

        #endregion
    }
}
