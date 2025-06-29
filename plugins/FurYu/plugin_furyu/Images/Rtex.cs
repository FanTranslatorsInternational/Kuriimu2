using Kanvas;
using Kanvas.Contract.Enums.Swizzle;
using Kanvas.Swizzle;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;
using ByteOrder = Komponent.Contract.Enums.ByteOrder;

namespace plugin_furyu.Images
{
    class Rtex
    {
        private const int HeaderSize = 0x20;
        private const int DataHeaderSize = 0x6;

        private RtexHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Decompress image data
            var decompStream = new MemoryStream();
            var compStream = new SubStream(input, _header.dataOffset + DataHeaderSize, _header.dataSize - DataHeaderSize);
            Compressions.ZLib.Build().Decompress(compStream, decompStream);

            // Create image info
            var imageInfo = new ImageFileInfo
            {
                BitDepth = RtexSupport.Formats[_header.format].BitDepth,
                ImageData = decompStream.ToArray(),
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height),
                PadSize = builder => builder.ToPowerOfTwo(),
                RemapPixels = context => new CtrSwizzle(context, CtrTransformation.YFlip)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataHeaderOffset = HeaderSize;
            var dataOffset = dataHeaderOffset + DataHeaderSize;

            // Compress image data
            output.Position = dataOffset;
            Compressions.ZLib.Build().Compress(new MemoryStream(imageInfo.ImageData), output);

            // Write data header
            var dataHeader = new RtexDataHeader
            {
                decompSize = imageInfo.ImageData.Length
            };

            output.Position = dataHeaderOffset;
            WriteDataHeader(dataHeader, bw);

            // Update header
            var paddedSize = SizePadding.PowerOfTwo(imageInfo.ImageSize);

            _header.dataOffset = dataHeaderOffset;
            _header.dataSize = (int)output.Length - dataHeaderOffset;
            _header.format = (byte)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;
            _header.paddedWidth = (short)paddedSize.Width;
            _header.paddedHeight = (short)paddedSize.Height;

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private RtexHeader ReadHeader(BinaryReaderX reader)
        {
            return new RtexHeader
            {
                magic = reader.ReadString(4),
                zero0 = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                paddedWidth = reader.ReadInt16(),
                paddedHeight = reader.ReadInt16(),
                dataOffset = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                format = reader.ReadByte(),
                unk1 = reader.ReadByte(),
                unk2 = reader.ReadInt16(),
                unk3 = reader.ReadInt32()
            };
        }

        private void WriteHeader(RtexHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.zero0);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.paddedWidth);
            writer.Write(header.paddedHeight);
            writer.Write(header.dataOffset);
            writer.Write(header.dataSize);
            writer.Write(header.format);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
        }

        private void WriteDataHeader(RtexDataHeader header, BinaryWriterX writer)
        {
            ByteOrder byteOrder = writer.ByteOrder;
            writer.ByteOrder = ByteOrder.BigEndian;

            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.decompSize);

            writer.ByteOrder = byteOrder;
        }
    }
}
