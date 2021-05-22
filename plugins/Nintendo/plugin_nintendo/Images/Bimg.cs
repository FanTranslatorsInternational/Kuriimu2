using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_nintendo.Images
{
    class Bimg
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(BimgHeader));

        private BimgHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<BimgHeader>();

            // Read image data
            var imgData = br.ReadBytes(_header.dataSize);

            // Create image info
            var imageInfo = new ImageInfo(imgData, _header.format, new Size(_header.width, _header.height));
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = HeaderSize;

            // Write image data
            output.Position = dataOffset;
            bw.Write(imageInfo.ImageData);

            // Update header
            _header.format = imageInfo.ImageFormat;
            _header.dataSize = imageInfo.ImageData.Length;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
