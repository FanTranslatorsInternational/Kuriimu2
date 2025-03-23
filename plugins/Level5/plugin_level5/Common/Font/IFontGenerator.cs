using Konnect.Contract.DataClasses.Plugin.File.Font;
using plugin_level5.Common.Font.Models;

namespace plugin_level5.Common.Font
{
    interface IFontGenerator
    {
        FontImageData Generate(FontImageData fontImageData, IList<CharacterInfo> characters);
    }
}
