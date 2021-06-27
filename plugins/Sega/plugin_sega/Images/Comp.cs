using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_sega.Images
{
    class Comp
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(CompHeader));

        private CompHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<CompHeader>();

            // Prepare image info
            var imageData = br.ReadBytes(_header.dataSize);

            // Create image info
            var imageInfo = new ImageInfo(imageData, _header.format, new Size(_header.width, _header.height));
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = HeaderSize;

            // Write image data
            output.Position = HeaderSize;
            output.Write(imageInfo.ImageData);

            // Update header
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;
            _header.format = (byte)imageInfo.ImageFormat;
            _header.dataSize = imageInfo.ImageData.Length;

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
