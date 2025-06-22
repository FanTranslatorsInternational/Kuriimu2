using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class GcBnr
    {
        private const int TitleInfoSize_ = 0x140;

        private GcBnrHeader _header;
        private IList<GcBnrTitleInfo> _titleInfos;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input, Encoding.GetEncoding("Shift-JIS"));

            // Read header
            _header = ReadHeader(br);
            br.SeekAlignment(0x20);

            // Read image data
            var imageData = br.ReadBytes(0x1800);

            // Read title info
            var titleInfoCount = (int)(input.Length - input.Position) / TitleInfoSize_;
            _titleInfos = ReadTitleInfos(br, titleInfoCount);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = 16,
                ImageData = imageData,
                ImageFormat = 0,
                ImageSize = new Size(96, 32),
                RemapPixels = context => new DolphinSwizzle(context)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output, Encoding.GetEncoding("Shift-JIS"));

            // Calculate offsets
            var imageDataOffset = 0x20;
            var titleInfoOffset = imageDataOffset + 0x1800;

            // Write title info
            output.Position = titleInfoOffset;
            WriteTitleInfos(_titleInfos, bw);

            // Write image data
            output.Position = imageDataOffset;
            output.Write(imageInfo.ImageData);

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private GcBnrHeader ReadHeader(BinaryReaderX reader)
        {
            return new GcBnrHeader
            {
                magic = reader.ReadString(4)
            };
        }

        private GcBnrTitleInfo[] ReadTitleInfos(BinaryReaderX reader, int count)
        {
            var result = new GcBnrTitleInfo[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadTitleInfo(reader);

            return result;
        }

        private GcBnrTitleInfo ReadTitleInfo(BinaryReaderX reader)
        {
            return new GcBnrTitleInfo
            {
                gameName = reader.ReadString(0x20),
                company = reader.ReadString(0x20),
                fullGameName = reader.ReadString(0x40),
                fullCompany = reader.ReadString(0x40),
                description = reader.ReadString(0x80)
            };
        }

        private void WriteHeader(GcBnrHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
        }

        private void WriteTitleInfos(IList<GcBnrTitleInfo> entries, BinaryWriterX writer)
        {
            foreach (GcBnrTitleInfo entry in entries)
                WriteTitleInfo(entry, writer);
        }

        private void WriteTitleInfo(GcBnrTitleInfo entry, BinaryWriterX writer)
        {
            writer.WriteString(entry.gameName, writeNullTerminator: false);
            writer.WriteString(entry.company, writeNullTerminator: false);
            writer.WriteString(entry.fullGameName, writeNullTerminator: false);
            writer.WriteString(entry.fullCompany, writeNullTerminator: false);
            writer.WriteString(entry.description, writeNullTerminator: false);
        }
    }
}
