using System.Reflection;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kuriimu2.ImGui.Resources
{
    static class ImageResources
    {
        #region Resource Names

        private const string IconResource_ = "Kuriimu2.ImGui.Resources.Images.kuriimu2.png";
        private const string CloseResource_ = "Kuriimu2.ImGui.Resources.Images.close.png";

        private const string SaveDarkResource_ = "Kuriimu2.ImGui.Resources.Images.dark.save.png";
        private const string SaveAsDarkResource_ = "Kuriimu2.ImGui.Resources.Images.dark.save_as.png";
        private const string ImageExportDarkResource_ = "Kuriimu2.ImGui.Resources.Images.dark.image_export.png";
        private const string ImageImportDarkResource_ = "Kuriimu2.ImGui.Resources.Images.dark.image_import.png";
        private const string BatchImageExportDarkResource_ = "Kuriimu2.ImGui.Resources.Images.dark.batch_image_export.png";
        private const string BatchImageImportDarkResource_ = "Kuriimu2.ImGui.Resources.Images.dark.batch_image_import.png";
        private const string ArrowRightDarkResource_ = "Kuriimu2.ImGui.Resources.Images.dark.arrow_right.png";

        private const string SaveLightResource_ = "Kuriimu2.ImGui.Resources.Images.light.save.png";
        private const string SaveAsLightResource_ = "Kuriimu2.ImGui.Resources.Images.light.save_as.png";
        private const string ImageExportLightResource_ = "Kuriimu2.ImGui.Resources.Images.light.image_export.png";
        private const string ImageImportLightResource_ = "Kuriimu2.ImGui.Resources.Images.light.image_import.png";
        private const string BatchImageExportLightResource_ = "Kuriimu2.ImGui.Resources.Images.light.batch_image_export.png";
        private const string BatchImageImportLightResource_ = "Kuriimu2.ImGui.Resources.Images.light.batch_image_import.png";
        private const string ArrowRightLightResource_ = "Kuriimu2.ImGui.Resources.Images.light.arrow_right.png";

        #endregion

        #region Resource Instances

        public static Image<Rgba32> Icon => FromResource(IconResource_);

        public static ThemedImageResource Close => new(GetImageResource(CloseResource_), GetImageResource(CloseResource_));

        public static ThemedImageResource Save => new(GetImageResource(SaveLightResource_), GetImageResource(SaveDarkResource_));

        public static ThemedImageResource SaveAs => new(GetImageResource(SaveAsLightResource_), GetImageResource(SaveAsDarkResource_));

        public static ThemedImageResource ImageExport => new(GetImageResource(ImageExportLightResource_), GetImageResource(ImageExportDarkResource_));

        public static ThemedImageResource ImageImport => new(GetImageResource(ImageImportLightResource_), GetImageResource(ImageImportDarkResource_));

        public static ThemedImageResource BatchImageExport => new(GetImageResource(BatchImageExportLightResource_), GetImageResource(BatchImageExportDarkResource_));

        public static ThemedImageResource BatchImageImport => new(GetImageResource(BatchImageImportLightResource_), GetImageResource(BatchImageImportDarkResource_));

        public static ThemedImageResource ArrowRight => new(GetImageResource(ArrowRightLightResource_), GetImageResource(ArrowRightDarkResource_));

        #endregion

        #region Support

        private static ImageResource GetImageResource(string name)
        {
            return ImageResource.FromResource(Assembly.GetExecutingAssembly(), name);
        }

        private static Image<Rgba32> FromResource(string name)
        {
            var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            return resourceStream == null ? null : Image.Load<Rgba32>(resourceStream);
        }

        #endregion
    }
}
