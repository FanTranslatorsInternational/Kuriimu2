using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_kadokawa.Images
{
    class Ctx
    {
        private CtxHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);
            br.SeekAlignment(0x20);

            // Read image data
            var imageData = br.ReadBytes(_header.dataSize);
            var imageInfo = new ImageFileInfo
            {
                BitDepth = CtxSupport.Formats[_header.format].BitDepth,
                ImageData = imageData,
                ImageFormat = unchecked((int)_header.format),
                ImageSize = new Size(_header.width, _header.height),
                RemapPixels = context => new CtrSwizzle(context)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = 0x40;

            // Write image data
            output.Position = dataOffset;
            bw.Write(imageInfo.ImageData);

            // Update header
            _header.dataSize = imageInfo.ImageData.Length;
            _header.format = unchecked((uint)imageInfo.ImageFormat);
            _header.width = imageInfo.ImageSize.Width;
            _header.height = imageInfo.ImageSize.Height;
            _header.width2 = imageInfo.ImageSize.Width;
            _header.height2 = imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private CtxHeader ReadHeader(BinaryReaderX reader)
        {
            return new CtxHeader
            {
                magic = reader.ReadString(8),
                width = reader.ReadInt32(),
                height = reader.ReadInt32(),
                width2 = reader.ReadInt32(),
                height2 = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                format = reader.ReadUInt32(),
                unk2 = reader.ReadInt32(),
                dataSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(CtxHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator:false);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.width2);
            writer.Write(header.height2);
            writer.Write(header.unk1);
            writer.Write(header.format);
            writer.Write(header.unk2);
            writer.Write(header.dataSize);
        }
    }
}
