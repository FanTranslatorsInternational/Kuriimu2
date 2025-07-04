using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_sega.Images
{
    class Comp
    {
        private static readonly int HeaderSize = 0x10;

        private CompHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Prepare image info
            var imageData = br.ReadBytes(_header.dataSize);

            // Create image info
            var imageInfo = new ImageFileInfo
            {
                BitDepth = CompSupport.Formats[_header.format].BitDepth,
                ImageData = imageData,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height),
                RemapPixels = context => new CtrSwizzle(context)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = HeaderSize;

            // Write image data
            output.Position = HeaderSize;
            output.Write(imageInfo.ImageData);

            // Update header
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;
            _header.format = (byte)imageInfo.ImageFormat;
            _header.dataSize = imageInfo.ImageData.Length;

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private CompHeader ReadHeader(BinaryReaderX reader)
        {
            return new CompHeader
            {
                dataSize = reader.ReadInt32(),
                format = reader.ReadByte(),
                unk1 = reader.ReadByte(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                zero0 = reader.ReadInt16(),
                zero1 = reader.ReadInt32()
            };
        }

        private void WriteHeader(CompHeader header, BinaryWriterX writer)
        {
            writer.Write(header.dataSize);
            writer.Write(header.format);
            writer.Write(header.unk1);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.zero0);
            writer.Write(header.zero1);
        }
    }
}
