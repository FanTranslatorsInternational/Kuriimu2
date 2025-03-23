using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Image
{
    public interface IImageWriter
    {
        void Write(ImageRawData data, Stream output);
    }
}
