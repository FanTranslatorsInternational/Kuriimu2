using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_arc_system_works.Images
{
    class Cvt
    {
        private CvtHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Create image info
            input.Position = 0x50;
            var imageData = br.ReadBytes((int)input.Length - 0x50);

            var imageInfo = new ImageFileInfo
            {
                Name = _header.name.Trim('\0'),
                BitDepth = CvtSupport.GetEncodingDefinition().GetColorEncoding(_header.format)?.BitDepth ?? 0,
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

            // Write image data
            output.Position = 0x50;
            output.Write(imageInfo.ImageData);

            // Update header
            _header.format = (short)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private CvtHeader ReadHeader(BinaryReaderX reader)
        {
            return new CvtHeader
            {
                magic = reader.ReadString(2),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                format = reader.ReadInt16(),
                unk1 = reader.ReadInt32(),
                name = reader.ReadString(0x20),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadInt32()
            };
        }

        private void WriteHeader(CvtHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.format);
            writer.Write(header.unk1);
            writer.WriteString(header.name, writeNullTerminator: false);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
        }
    }
}
