using Konnect.Contract.Plugin.File.Image;

namespace plugin_level5.Common.Image.Models
{
    public class ImageData
    {
        public FormatVersion Version { get; set; }

        public IImageFile Image { get; set; }
        public IImageFile[] Mipmaps { get; set; }

        public byte[]? LegacyData { get; set; }
    }
}
