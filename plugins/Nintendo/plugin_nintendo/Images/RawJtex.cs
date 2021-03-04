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

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            var header = br.ReadType<RawJtexHeader>();

            // Read images
            input.Position = header.dataOffset;
            var info = new ImageInfo(br.ReadBytes((int)(input.Length - header.dataOffset)), header.format, new Size(header.width, header.height));

            info.RemapPixels.With(context => new CtrSwizzle(context));
            info.PadSize.ToPowerOfTwo();

            return info;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var texDataOffset = (HeaderSize + 0x7F) & ~0x7F;

            // Write image data
            output.Position = texDataOffset;
            output.Write(imageInfo.ImageData);

            // Write header
            var paddedSize = imageInfo.PadSize.Build(imageInfo.ImageSize);
            var header = new RawJtexHeader
            {
                dataOffset = (uint)texDataOffset,
                format = imageInfo.ImageFormat,
                width = imageInfo.ImageSize.Width,
                height = imageInfo.ImageSize.Height,
                paddedWidth = paddedSize.Width,
                paddedHeight = paddedSize.Height
            };

            output.Position = 0;
            bw.WriteType(header);
        }
    }
}
