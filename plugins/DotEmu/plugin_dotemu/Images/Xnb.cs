using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_dotemu.Images
{
    class Xnb
    {
        private const int HeaderSize = 0x25;

        private XnbHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Read image info
            var imgData = br.ReadBytes(_header.dataSize);
            var imageInfo = new ImageFileInfo
            {
                BitDepth = XnbSupport.Formats[_header.format].BitDepth,
                ImageData = imgData,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height)
            };

            if (XnbSupport.Formats[_header.format].ColorsPerValue > 1)
                imageInfo.RemapPixels = context => new BcSwizzle(context);

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Write class name
            bw.BaseStream.Position = 0xB;
            bw.Write(_header.className);

            long classNameLength = bw.BaseStream.Position - 0xB;
            long headerSize = HeaderSize + classNameLength;

            // Write header
            _header.format = imageInfo.ImageFormat;
            _header.dataSize = imageInfo.ImageData.Length;
            _header.fileSize = (int)(imageInfo.ImageData.Length + headerSize);
            _header.width = imageInfo.ImageSize.Width;
            _header.height = imageInfo.ImageSize.Height;

            bw.BaseStream.Position = 0;
            WriteHeader(_header, bw);

            // Write image data
            bw.Write(imageInfo.ImageData);
        }

        private XnbHeader ReadHeader(BinaryReaderX reader)
        {
            return new XnbHeader
            {
                magic = reader.ReadString(4),
                major = reader.ReadByte(),
                minor = reader.ReadByte(),
                fileSize = reader.ReadInt32(),
                itemCount = reader.ReadByte(),
                className = reader.ReadString(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt16(),
                format = reader.ReadInt32(),
                width = reader.ReadInt32(),
                height = reader.ReadInt32(),
                mipCount = reader.ReadInt32(),
                dataSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(XnbHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.major);
            writer.Write(header.minor);
            writer.Write(header.fileSize);
            writer.Write(header.itemCount);
            writer.Write(header.className);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.format);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.mipCount);
            writer.Write(header.dataSize);
        }
    }
}
