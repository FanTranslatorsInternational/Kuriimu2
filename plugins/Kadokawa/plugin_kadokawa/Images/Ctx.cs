using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_kadokawa.Images
{
    class Ctx
    {
        private CtxHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<CtxHeader>();

            // Read image data
            var imageData = br.ReadBytes(_header.dataSize);
            var imageInfo = new ImageInfo(imageData, _header.format, new Size(_header.width, _header.height));

            imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = 0x40;

            // Write image data
            output.Position = dataOffset;
            bw.Write(imageInfo.ImageData);

            // Update header
            _header.dataSize = imageInfo.ImageData.Length;
            _header.format = imageInfo.ImageFormat;
            _header.width = imageInfo.ImageSize.Width;
            _header.height = imageInfo.ImageSize.Height;
            _header.width2 = imageInfo.ImageSize.Width;
            _header.height2 = imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
