using plugin_level5.Common.Archive.Models;
using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Font.Models
{
    public class FontImageData
    {
        public ArchiveType ArchiveType { get; set; }
        public PlatformType Platform { get; set; }
        public FontData Font { get; set; }
        public ImageData[] Images { get; set; }
    }
}
