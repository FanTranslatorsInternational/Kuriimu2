using Kanvas.Contract.Enums.Swizzle;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_spike_chunsoft.Images
{
    class Cte
    {
        private CteHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Read image data
            input.Position = _header.dataOffset;
            var imgData = br.ReadBytes((int)(input.Length - _header.dataOffset));

            // Create image info
            var imageInfo = new ImageFileInfo
            {
                BitDepth = CteSupport.Formats[_header.format].BitDepth,
                ImageData = imgData,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height),
                RemapPixels = context => new CtrSwizzle(context, CtrTransformation.YFlip)
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
            _header.dataOffset = dataOffset;
            _header.format = imageInfo.ImageFormat;
            _header.format2 = imageInfo.ImageFormat;
            _header.width = imageInfo.ImageSize.Width;
            _header.height = imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private CteHeader ReadHeader(BinaryReaderX reader)
        {
            return new CteHeader
            {
                magic = reader.ReadString(4),
                format = reader.ReadInt32(),
                width = reader.ReadInt32(),
                height = reader.ReadInt32(),
                format2 = reader.ReadInt32(),
                zero1 = reader.ReadInt32(),
                dataOffset = reader.ReadInt32()
            };
        }

        private void WriteHeader(CteHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.format);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.format2);
            writer.Write(header.zero1);
            writer.Write(header.dataOffset);
        }
    }
}
