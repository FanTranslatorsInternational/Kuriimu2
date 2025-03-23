using plugin_level5.Common.Font.Models;

namespace plugin_level5.Common.Font
{
    public interface IFontWriter
    {
        void Write(FontData font, Stream output);
    }
}
