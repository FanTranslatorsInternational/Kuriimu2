using System;
using System.Drawing;
using System.IO;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_dotemu.Images
{
    class Sdt
    {
        private SdtHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<SdtHeader>();

            // Read image info
            var imgData = br.ReadBytes(_header.imageSize);
            var imageInfo = new ImageInfo(imgData, _header.unk1, new Size(_header.width, _header.height));
            var paletteData = br.ReadBytes(_header.paletteSize);
            imageInfo.PadSize.Width.ToPowerOfTwo();

            imageInfo.PaletteData = paletteData;
            imageInfo.PaletteFormat = 0;
            return imageInfo;
        }
    }
}
