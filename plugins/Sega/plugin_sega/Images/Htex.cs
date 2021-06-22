using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_sega.Images
{
    class Htex
    {
        public ImageInfo Load(Stream input)
        {
            using var br=new BinaryReaderX(input);

            input.Position = 0x2C;
            var width = br.ReadInt16();
            var height = br.ReadInt16();

            var paletteData = br.ReadBytes(4 * 256);
            var imageData = br.ReadBytes(width * height);

            var imageInfo= new ImageInfo(imageData,0,new Size(width,height))
            {
                PaletteData = paletteData,
                PaletteFormat = 0
            };

            imageInfo.RemapPixels.With(context => new Ps2Swizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {

        }
    }
}
