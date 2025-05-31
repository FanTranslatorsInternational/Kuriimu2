using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;
using System.Text;

namespace plugin_atlus.N3DS.Image
{
    public class Stex
    {
        private static readonly int HeaderSize = 32;
        private static readonly int EntrySize = 8;
        private static int unk1;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            var header = ReadHeader(br);

            // Read entry
            var entry = ReadEntry(br);

            // Quick hack (We will probably replace this in the future)
            unk1 = entry.unk1;

            // Read name
            var name = br.ReadNullTerminatedString();

            // Create image info
            input.Position = entry.offset;
            var imageData = br.ReadBytes(header.dataSize);

            var format = (header.dataType << 16) | header.imageFormat;

            var imageInfo = new ImageFileInfo
            {
                Name = name,
                BitDepth = imageData.Length * 8 / (header.width * header.height),
                ImageData = imageData,
                ImageFormat = (int)format,
                ImageSize = new Size(header.width, header.height),
                RemapPixels = context => new CtrSwizzle(context),
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var nameOffset = entryOffset + EntrySize;
            var dataOffset = (nameOffset + Encoding.ASCII.GetByteCount(imageInfo.Name) + 1 + 0x7F) & ~0x7F;

            // Write image data
            output.Position = dataOffset;
            output.Write(imageInfo.ImageData);

            // Write name
            output.Position = nameOffset;
            bw.WriteString(imageInfo.Name, Encoding.ASCII);

            // Write entry
            var entry = new StexEntry
            {
                offset = dataOffset,
                unk1 = unk1
            };

            output.Position = entryOffset;
            WriteEntry(entry, bw);

            // Write header
            var header = new StexHeader
            {
                magic = "STEX",
                const0 = 0xDE1,
                width = imageInfo.ImageSize.Width,
                height = imageInfo.ImageSize.Height,
                dataSize = (int)(output.Length - dataOffset),
                dataType = (uint)((imageInfo.ImageFormat >> 16) & 0xFFFF),
                imageFormat = (uint)(imageInfo.ImageFormat & 0xFFFF)
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private StexHeader ReadHeader(BinaryReaderX reader)
        {
            return new StexHeader
            {
                magic = reader.ReadString(4),
                zero0 = reader.ReadUInt32(),
                const0 = reader.ReadUInt32(),
                width = reader.ReadInt32(),
                height = reader.ReadInt32(),
                dataType = reader.ReadUInt32(),
                imageFormat = reader.ReadUInt32(),
                dataSize = reader.ReadInt32()
            };
        }

        private StexEntry ReadEntry(BinaryReaderX reader)
        {
            return new StexEntry
            {
                offset = reader.ReadInt32(),
                unk1 = reader.ReadInt32()
            };
        }

        private void WriteHeader(StexHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.zero0);
            writer.Write(header.const0);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.dataType);
            writer.Write(header.imageFormat);
            writer.Write(header.dataSize);
        }

        private void WriteEntry(StexEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.unk1);
        }
    }
}
