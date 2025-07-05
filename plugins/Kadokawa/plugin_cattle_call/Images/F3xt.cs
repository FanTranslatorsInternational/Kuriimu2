using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_cattle_call.Images
{
    class F3xt
    {
        private F3xtHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Read image data
            var dataSize = (int)(input.Length - _header.dataStart);

            input.Position = _header.dataStart;
            var imageData = br.ReadBytes(dataSize);

            // Create image info
            var imageInfo = new ImageFileInfo
            {
                BitDepth = F3xtSupport.Formats[_header.format].BitDepth,
                ImageData = imageData,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height),
                PadSize = builder => builder.Width.To(_ => _header.paddedWidth).Height.To(_ => _header.paddedHeight),
                RemapPixels = context => new CtrSwizzle(context)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = 0x80;

            // Write image data
            output.Position = dataOffset;
            bw.Write(imageInfo.ImageData);

            // Update header
            _header.dataStart = (uint)dataOffset;
            _header.width = (ushort)imageInfo.ImageSize.Width;
            _header.height = (ushort)imageInfo.ImageSize.Height;
            _header.format = (short)imageInfo.ImageFormat;

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private F3xtHeader ReadHeader(BinaryReaderX reader)
        {
            return new F3xtHeader
            {
                magic = reader.ReadString(4),
                texEntries = reader.ReadUInt32(),
                format = reader.ReadInt16(),
                widthLog = reader.ReadByte(),
                heightLog = reader.ReadByte(),
                width = reader.ReadUInt16(),
                height = reader.ReadUInt16(),
                paddedWidth = reader.ReadUInt16(),
                paddedHeight = reader.ReadUInt16(),
                dataStart = reader.ReadUInt32()
            };
        }

        private void WriteHeader(F3xtHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.texEntries);
            writer.Write(header.format);
            writer.Write(header.widthLog);
            writer.Write(header.heightLog);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.paddedWidth);
            writer.Write(header.paddedHeight);
            writer.Write(header.dataStart);
        }
    }
}
