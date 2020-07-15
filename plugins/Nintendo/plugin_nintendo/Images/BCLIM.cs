using System.Drawing;
using System.IO;
using Kanvas.Configuration;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;
using plugin_nintendo.BCLIM;
using plugin_nintendo.NW4C;

namespace plugin_nintendo.Images
{
    public class Bclim
    {
        private static readonly int Nw4CHeaderSize = Tools.MeasureType(typeof(NW4CHeader));
        private static readonly int BclimHeaderSize = Tools.MeasureType(typeof(BclimHeader));

        private NW4CHeader _header;
        private BclimHeader _textureHeader;

        public ImageInfo Load(Stream input)
        {
            var dataLength = (int)input.Length - (Nw4CHeaderSize + BclimHeaderSize);

            using (var br = new BinaryReaderX(input))
            {
                var textureData = br.ReadBytes(dataLength);

                _header = br.ReadType<NW4CHeader>();
                br.ByteOrder = _header.ByteOrder;

                _textureHeader = br.ReadType<BclimHeader>();

                var imageInfo = new ImageInfo
                {
                    ImageData = textureData,
                    ImageFormat = _textureHeader.Format,
                    ImageSize = new Size(_textureHeader.Width, _textureHeader.Height),
                    Configuration = new ImageConfiguration().
                        RemapPixelsWith(size => new CTRSwizzle(_textureHeader.Width, _textureHeader.Height, _textureHeader.SwizzleTileMode, true))
                };

                return imageInfo;
            }
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {

        }
    }
}
