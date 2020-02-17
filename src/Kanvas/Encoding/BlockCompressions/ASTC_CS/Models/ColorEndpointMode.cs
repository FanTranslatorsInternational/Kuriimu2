using Kanvas.Encoding.BlockCompressions.ASTC_CS.Types;
using Komponent.IO;

namespace Kanvas.Encoding.BlockCompressions.ASTC_CS.Models
{
    class ColorEndpointMode
    {
        public ColorFormat Format { get; }

        public bool IsHdr { get; }

        public int Class { get; }

        public int EndpointValueCount { get; }

        public static ColorEndpointMode Create(BitReader br)
        {
            return new ColorEndpointMode(br.ReadBits<int>(4));
        }

        private ColorEndpointMode(int value)
        {
            Format = (ColorFormat)value;
            IsHdr = Format == ColorFormat.FmtHdrRgb ||
                    Format == ColorFormat.FmtHdrRgba ||
                    Format == ColorFormat.FmtHdrRgbScale ||
                    Format == ColorFormat.FmtHdrRgbLdrAlpha ||
                    Format == ColorFormat.FmtHdrLuminanceLargeRange ||
                    Format == ColorFormat.FmtHdrLuminanceSmallRange;
            Class = value / 4;
            EndpointValueCount = (Class + 1) * 2;
        }
    }
}
