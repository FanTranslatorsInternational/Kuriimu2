using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_ganbarion.Images
{
    class Jtex
    {
        private JtexHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<JtexHeader>();

            // Create image info
            input.Position = 0x80;
            var imageData = br.ReadBytes(_header.dataSize);

            var size = new Size(_header.width == 0 ? _header.paddedWidth : _header.width, _header.height == 0 ? _header.paddedHeight : _header.height);

            var imageInfo = new ImageInfo(imageData, _header.format, size);
            imageInfo.PadSize.ToPowerOfTwo();
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            var paddedSize = imageInfo.PadSize.Build(imageInfo.ImageSize);

            // Write image data
            output.Position = 0x80;
            output.Write(imageInfo.ImageData);

            // Update header
            _header.paddedWidth = (short)paddedSize.Width;
            _header.paddedHeight = (short)paddedSize.Height;
            _header.dataSize = imageInfo.ImageData.Length;
            _header.format = (byte)imageInfo.ImageFormat;
            _header.fileSize = (int)output.Length;
            _header.width = (short)(imageInfo.ImageSize.Width == paddedSize.Width ? 0 : imageInfo.ImageSize.Width);
            _header.width = (short)(imageInfo.ImageSize.Height == paddedSize.Height ? 0 : imageInfo.ImageSize.Height);

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
