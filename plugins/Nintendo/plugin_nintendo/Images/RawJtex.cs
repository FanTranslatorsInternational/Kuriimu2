using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_nintendo.Images
{
    class RawJtex
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(RawJtexHeader));

        private bool _shouldAlign;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read data offset
            var dataOffset = br.ReadInt32();
            _shouldAlign = dataOffset == 0x80;

            if (dataOffset != 0x80)
            {
                dataOffset = HeaderSize;
                input.Position -= 4;
            }

            // Read header
            var header = br.ReadType<RawJtexHeader>();

            // Read images
            input.Position = dataOffset;
            var info = new ImageInfo(br.ReadBytes((int)(input.Length - dataOffset)), header.format, new Size(header.width, header.height));

            info.RemapPixels.With(context => new CtrSwizzle(context));
            info.PadSize.ToPowerOfTwo();

            return info;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output, true);

            // Calculate offsets
            var texDataOffset = _shouldAlign ? (HeaderSize + 0x7F) & ~0x7F : HeaderSize;

            // Write image data
            output.Position = texDataOffset;
            output.Write(imageInfo.ImageData);

            // Update header
            var paddedSize = imageInfo.PadSize.Build(imageInfo.ImageSize);
            var header = new RawJtexHeader
            {
                format = imageInfo.ImageFormat,
                width = imageInfo.ImageSize.Width,
                height = imageInfo.ImageSize.Height,
                paddedWidth = paddedSize.Width,
                paddedHeight = paddedSize.Height
            };

            // Write header
            output.Position = 0;
            if (_shouldAlign) bw.Write(texDataOffset);
            bw.WriteType(header);
        }
    }
}
