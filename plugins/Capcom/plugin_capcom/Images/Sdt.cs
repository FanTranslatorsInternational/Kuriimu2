using Komponent.IO;
using Kontract.Models.Image;
using System.Drawing;
using System.IO;

namespace plugin_dotemu.Images
{
    class Sdt
    {
        private SdtHeader _header;

        public ImageInfo Load(Stream input)
        {
            using BinaryReaderX br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<SdtHeader>();

            // Read image info
            byte[] imgData = br.ReadBytes(_header.imageSize);
            ImageInfo imageInfo = new ImageInfo(imgData, _header.format, new Size(_header.width, _header.height));
            byte[] paletteData = br.ReadBytes(_header.paletteSize);
            imageInfo.PadSize.Width.ToPowerOfTwo();

            imageInfo.PaletteData = paletteData;
            imageInfo.PaletteFormat = 0;
            return imageInfo;
        }
    }
}
