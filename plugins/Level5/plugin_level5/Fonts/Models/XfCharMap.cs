using System.Diagnostics;

namespace plugin_level5.Fonts.Models
{
    [DebuggerDisplay("[{codePoint}; Image: {imageInformation.imageOffsetX},{imageInformation.imageOffsetY}]")]
    public class XfCharMap
    {
        public ushort codePoint;
        public XfCharInformation charInformation;
        public XfImageInformation imageInformation;
    }
}
