using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class Bimg
    {
        private const int HeaderSize_ = 0x20;

        private BimgHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Read image data
            var imgData = br.ReadBytes(_header.dataSize);

            // Create image info
            var imageInfo = new ImageFileInfo
            {
                BitDepth = BimgSupport.GetEncodingDefinition().GetColorEncoding(_header.format).BitDepth,
                ImageData = imgData,
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
            var dataOffset = HeaderSize_;

            // Write image data
            output.Position = dataOffset;
            bw.Write(imageInfo.ImageData);

            // Update header
            _header.format = imageInfo.ImageFormat;
            _header.dataSize = imageInfo.ImageData.Length;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private BimgHeader ReadHeader(BinaryReaderX reader)
        {
            return new BimgHeader
            {
                zero1 = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                zero2 = reader.ReadInt32(),
                format = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadUInt32()
            };
        }

        private void WriteHeader(BimgHeader header, BinaryWriterX writer)
        {
            writer.Write(header.zero1);
            writer.Write(header.dataSize);
            writer.Write(header.zero2);
            writer.Write(header.format);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
        }
    }
}
