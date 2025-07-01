using System.Text;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_superflat_games.Images
{
    class Img
    {
        private static readonly int HeaderSize = 0xC;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read image data
            var imgHeader = ReadHeader(br);
            var imgData = br.ReadBytes(imgHeader.size);

            // Read tex info
            _ = ReadHeader(br);
            var texInfo = ReadEntry(br);

            return new ImageFileInfo
            {
                BitDepth = 32,
                ImageData = imgData,
                ImageFormat = 0,
                ImageSize = new Size(texInfo.width, texInfo.height),
            };
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var texOffset = HeaderSize + imageInfo.ImageData.Length;

            // Write image data
            WriteHeader(new ImgHeader { magic = "IMG0", size = imageInfo.ImageData.Length }, bw);
            bw.Write(imageInfo.ImageData);

            // Write tex header
            WriteHeader(new ImgHeader { magic = "TEXR", size = 0x10 }, bw);
            WriteEntry(new ImgEntry { width = imageInfo.ImageSize.Width, height = imageInfo.ImageSize.Height }, bw);

            // Write end header
            bw.WriteString("!END", Encoding.ASCII, false, false);
            bw.WritePadding(3);
        }

        private ImgHeader ReadHeader(BinaryReaderX reader)
        {
            return new ImgHeader
            {
                magic = reader.ReadString(4),
                size = reader.ReadInt32(),
                zero0 = reader.ReadInt32()
            };
        }

        private ImgEntry ReadEntry(BinaryReaderX reader)
        {
            return new ImgEntry
            {
                width = reader.ReadInt32(),
                height = reader.ReadInt32(),
                zero0 = reader.ReadInt32(),
                zero1 = reader.ReadInt32()
            };
        }

        private void WriteHeader(ImgHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.size);
            writer.Write(header.zero0);
        }

        private void WriteEntry(ImgEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.width);
            writer.Write(entry.height);
            writer.Write(entry.zero0);
            writer.Write(entry.zero1);
        }
    }
}
