using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_cattle_call.Images
{
    class F3xt
    {
        private F3xtHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<F3xtHeader>();

            // Read image data
            var dataSize = (int)(input.Length - _header.dataStart);

            input.Position = _header.dataStart;
            var imageData = br.ReadBytes(dataSize);

            // Create image info
            var imageInfo = new ImageInfo(imageData, _header.format, new Size(_header.width, _header.height));
            imageInfo.PadSize.Width.To(dimension => _header.paddedWidth).Height.To(dimension => _header.paddedHeight);
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
            _header.dataStart = (uint)dataOffset;
            _header.width = (ushort)imageInfo.ImageSize.Width;
            _header.height = (ushort)imageInfo.ImageSize.Height;
            _header.format = (short)imageInfo.ImageFormat;

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
