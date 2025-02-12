using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_nintendo.Images
{
    class _3dst
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(_3dstHeader));

        private _3dstHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<_3dstHeader>();

            // Read image data
            br.BaseStream.Position = 0x80;
            var imgData = br.ReadBytes((int)(input.Length - 0x80));

            // Create image info
            var imageInfo = new ImageInfo(imgData, _header.format, new Size(_header.width, _header.height));
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = 0x80;

            // Write image data
            output.Position = dataOffset;
            bw.Write(imageInfo.ImageData);

            // Update header
            _header.format = (short)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
