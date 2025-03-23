using plugin_level5.Common.Font.Models;

namespace plugin_level5.Common.Font
{
    public interface IFontReader
    {
        FontData Read(Stream input);
    }
}
