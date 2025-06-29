using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_felistella.Images
{
    class Tex
    {
        private TexHeader _header;
        private TexEntry _entry;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Read entry
            input.Position = _header.entryOffset;
            _entry = ReadEntry(br);

            // Create image info
            input.Position = _entry.dataOffset;
            var imgData = br.ReadBytes(_entry.dataSize);

            return new ImageFileInfo
            {
                BitDepth = TexSupport.Formats[0].BitDepth,
                ImageData = imgData,
                ImageFormat = 0,
                ImageSize = new Size(_entry.width, _entry.height)
            };
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Write image data
            output.Position = _entry.dataOffset;
            bw.Write(imageInfo.ImageData);
            bw.WritePadding(0x10);

            // Write entry
            _entry.width = (short)imageInfo.ImageSize.Width;
            _entry.height = (short)imageInfo.ImageSize.Height;
            _entry.dataSize = imageInfo.ImageData.Length;

            output.Position = _header.entryOffset;
            WriteEntry(_entry, bw);

            bw.Write(1);

            // Write header
            _header.dataSize = imageInfo.ImageData.Length + 0x20;
            _header.fileSize = (int)output.Length;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private TexHeader ReadHeader(BinaryReaderX reader)
        {
            return new TexHeader
            {
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                fileSize = reader.ReadInt32(),
                unk3 = reader.ReadUInt32(),
                headerSize = reader.ReadInt32(),
                dataStart = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                zero0 = reader.ReadInt32(),
                entryOffset = reader.ReadInt32(),
                unk4 = reader.ReadInt32()
            };
        }

        private TexEntry ReadEntry(BinaryReaderX reader)
        {
            return new TexEntry
            {
                unk1 = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                dataOffset = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                zero0 = reader.ReadInt32(),
                zero1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadInt32(),
            };
        }

        private void WriteHeader(TexHeader header, BinaryWriterX writer)
        {
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.fileSize);
            writer.Write(header.unk3);
            writer.Write(header.headerSize);
            writer.Write(header.dataStart);
            writer.Write(header.dataSize);
            writer.Write(header.zero0);
            writer.Write(header.entryOffset);
            writer.Write(header.unk4);
        }

        private void WriteEntry(TexEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.unk1);
            writer.Write(entry.width);
            writer.Write(entry.height);
            writer.Write(entry.dataOffset);
            writer.Write(entry.dataSize);
            writer.Write(entry.zero0);
            writer.Write(entry.zero1);
            writer.Write(entry.unk2);
            writer.Write(entry.unk3);
        }
    }
}
