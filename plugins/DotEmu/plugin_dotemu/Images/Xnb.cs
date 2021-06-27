using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_dotemu.Images
{
    class Xnb
    {
        private const int HeaderSize = 0x55;

        private XnbHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<XnbHeader>();

            // Read image info
            var imgData = br.ReadBytes(_header.dataSize);
            var imageInfo = new ImageInfo(imgData, _header.format, new Size(_header.width, _header.height));

            if (XnbSupport.Formats[_header.format].ColorsPerValue > 1)
                imageInfo.RemapPixels.With(context => new BcSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Write header
            _header.format = imageInfo.ImageFormat;
            _header.dataSize = imageInfo.ImageData.Length;
            _header.fileSize = imageInfo.ImageData.Length + HeaderSize;
            _header.width = imageInfo.ImageSize.Width;
            _header.height = imageInfo.ImageSize.Height;

            bw.WriteType(_header);

            // Write image data
            bw.Write(imageInfo.ImageData);
        }
    }
}
