using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class RawJtex
    {
        private const int HeaderSize_ = 0x14;

        private bool _shouldAlign;

        public ImageFileInfo Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input);

            // Read data offset
            var dataOffset = br.ReadInt32();
            _shouldAlign = dataOffset == 0x80;

            if (dataOffset != 0x80)
            {
                dataOffset = HeaderSize_;
                input.Position -= 4;
            }

            // Read header
            var header = typeReader.Read<RawJtexHeader>(br);

            // Read images
            input.Position = dataOffset;
            var info = new ImageFileInfo
            {
                BitDepth = RawJtexSupport.GetEncodingDefinition().GetColorEncoding(header.format).BitDepth,
                ImageData = br.ReadBytes((int)(input.Length - dataOffset)),
                ImageFormat = header.format,
                ImageSize = new Size(header.width, header.height),
                RemapPixels = context => new CtrSwizzle(context),
                PadSize = builder => builder.ToPowerOfTwo()
            };

            return info;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output, true);

            // Calculate offsets
            var texDataOffset = _shouldAlign ? (HeaderSize_ + 0x7F) & ~0x7F : HeaderSize_;

            // Write image data
            output.Position = texDataOffset;
            output.Write(imageInfo.ImageData);

            // Update header
            var paddedSize = Kanvas.SizePadding.PowerOfTwo(imageInfo.ImageSize);
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
            typeWriter.Write(header, bw);
        }
    }
}
