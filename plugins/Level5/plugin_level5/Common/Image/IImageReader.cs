using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Image
{
    public interface IImageReader
    {
        ImageRawData Read(Stream input);
    }
}
