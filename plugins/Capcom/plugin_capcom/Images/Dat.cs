using Komponent.IO;
using Kontract.Models.Image;
using System.Drawing;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using System.IO;

namespace plugin_dotemu.Images
{
    class Dat
    {
        private DatHeader _header;

        public ImageInfo Load(Stream input)
        {


                using BinaryReaderX br = new BinaryReaderX(input);
            try
            {
                // Read header
                _header = br.ReadType<DatHeader>();
                var _datConstants = new DatConstants();
                br.BaseStream.Position = _datConstants.paletteOffset;
                byte[] paletteData = br.ReadBytes(_datConstants.paletteOffset);
                // Read image info
                var imageSize = ((int)input.Length) - 0x400;
                byte[] imgData = br.ReadBytes(imageSize);
                var imageInfo = new ImageInfo(imgData, 0, new Size(_header.width, _header.height));
                imageInfo.RemapPixels.With(context => new DatSwizzle(context));
                //imageInfo.PadSize.Width.ToPowerOfTwo();

                imageInfo.PaletteData = paletteData;
                imageInfo.PaletteFormat = 0;
                return imageInfo;
            }
            catch
            {
                byte[] imgData = new byte[0];
                System.Diagnostics.Debug.WriteLine("I am in the Catch");
                var imageInfo = new ImageInfo(imgData, 0, new Size(0, 0));
                return imageInfo;
            }
        }
    }
}
