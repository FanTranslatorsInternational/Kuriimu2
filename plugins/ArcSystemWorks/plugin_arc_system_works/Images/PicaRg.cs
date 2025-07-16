using Kanvas.Contract.Enums;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_arc_system_works.Images
{
    class PicaRg
    {
        private static readonly int HeaderSize = 0x10;

        private PicaRgHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Read image data
            var imageData = br.ReadBytes((int)(input.Length - HeaderSize));

            // Create image info
            var imageInfo = new ImageFileInfo
            {
                BitDepth = PicaRgSupport.GetEncodingDefinition().GetColorEncoding(_header.format)?.BitDepth ?? 0,
                ImageData = imageData,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height),
                IsAnchoredAt = ImageAnchor.BottomLeft,
                RemapPixels = context => new CtrSwizzle(context),
                PadSize = builder => builder.ToPowerOfTwo()
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = HeaderSize;

            // Write image data
            output.Position = dataOffset;
            output.Write(imageInfo.ImageData);

            // Write header
            _header.format = (ushort)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;

            var paddedSize = Kanvas.SizePadding.PowerOfTwo(imageInfo.ImageSize);
            _header.paddedWidth = (short)paddedSize.Width;
            _header.paddedHeight = (short)paddedSize.Height;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private PicaRgHeader ReadHeader(BinaryReaderX reader)
        {
            return new PicaRgHeader
            {
                magic = reader.ReadString(6),
                format = reader.ReadUInt16(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                paddedWidth = reader.ReadInt16(),
                paddedHeight = reader.ReadInt16()
            };
        }

        private void WriteHeader(PicaRgHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.format);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.paddedWidth);
            writer.Write(header.paddedHeight);
        }
    }
}
