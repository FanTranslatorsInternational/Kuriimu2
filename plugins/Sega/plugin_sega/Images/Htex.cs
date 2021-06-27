using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_sega.Images
{
    class Htex
    {
        private bool _hasGbix;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read 2 HTEX headers
            br.ReadType<HtexHeader>();
            br.ReadType<HtexHeader>();

            // Skip GBIX header
            var texHeader = br.ReadType<HtexHeader>();
            _hasGbix = texHeader.magic == "GBIX";
            if (_hasGbix)
                texHeader = br.ReadType<HtexHeader>();

            var format = texHeader.data1;
            var width = (int)(texHeader.data2 >> 16);
            var height = (int)(texHeader.data2 & 0xFFFF);

            var paletteData = br.ReadBytes(4 * 256);
            var imageData = br.ReadBytes(width * height);

            var imageInfo = new ImageInfo(imageData, 0, new Size(width, height))
            {
                PaletteData = paletteData,
                PaletteFormat = (int)format
            };

            imageInfo.RemapPixels.With(context => new Ps2Swizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {

        }
    }
}
