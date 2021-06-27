using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_spike_chunsoft.Images
{
    class Cte
    {
        private CteHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<CteHeader>();

            // Read image data
            input.Position = _header.dataOffset;
            var imgData = br.ReadBytes((int)(input.Length - _header.dataOffset));

            // Create image info
            var imageInfo = new ImageInfo(imgData, _header.format, new Size(_header.width, _header.height));
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context, CtrTransformation.YFlip));

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
            _header.dataOffset = dataOffset;
            _header.format = imageInfo.ImageFormat;
            _header.format2 = imageInfo.ImageFormat;
            _header.width = imageInfo.ImageSize.Width;
            _header.height = imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
