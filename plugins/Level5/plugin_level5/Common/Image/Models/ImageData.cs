using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Image;

namespace plugin_level5.Common.Image.Models
{
    public class ImageData
    {
        public FormatVersion Version { get; set; }

        public IImageFile Image { get; set; }

        public byte[]? LegacyData { get; set; }

        public IFileState? KtxState { get; set; }
    }
}
