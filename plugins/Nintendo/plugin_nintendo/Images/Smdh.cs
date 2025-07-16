using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class Smdh
    {
        private const int HeaderSize_ = 0x8;
        private const int AppTitleSize_ = 0x200;
        private const int AppSettingsSize_ = 0x30;

        private SmdhHeader _header;
        private IList<SmdhApplicationTitle> _appTitles;
        private SmdhAppSettings _settings;

        public List<ImageFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, Encoding.Unicode);

            // Read header
            _header = ReadHeader(br);

            // Read application titles
            _appTitles = ReadTitles(br, 0x10);

            // Read application settings
            _settings = ReadSettings(br);
            br.BaseStream.Position += 0x8;

            // Read image data
            var result = new List<ImageFileInfo>();

            var imageData = br.ReadBytes(0x480);
            result.Add(new ImageFileInfo
            {
                BitDepth = 16,
                ImageData = imageData,
                ImageFormat = 0,
                ImageSize = new Size(24, 24),
                RemapPixels = context => new CtrSwizzle(context)
            });

            imageData = br.ReadBytes(0x1200);
            result.Add(new ImageFileInfo
            {
                BitDepth = 16,
                ImageData = imageData,
                ImageFormat = 0,
                ImageSize = new Size(48, 48),
                RemapPixels = context => new CtrSwizzle(context)
            });

            return result;
        }

        public void Save(Stream output, List<ImageFileInfo> imageInfos)
        {
            using var bw = new BinaryWriterX(output, Encoding.Unicode);

            // Calculate offsets
            var dataOffset = (HeaderSize_ + _appTitles.Count * AppTitleSize_ + AppSettingsSize_ + 0xF) & ~0xF;

            // Write image data
            output.Position = dataOffset;
            foreach (var imageInfo in imageInfos.OrderBy(x => x.ImageSize.Width))
                bw.Write(imageInfo.ImageData);

            // Write icon information
            output.Position = 0;
            WriteHeader(_header, bw);
            WriteTitles(_appTitles, bw);
            WriteSettings(_settings, bw);
        }

        private SmdhHeader ReadHeader(BinaryReaderX reader)
        {
            return new SmdhHeader
            {
                magic = reader.ReadString(4, Encoding.ASCII),
                version = reader.ReadInt16(),
                reserved = reader.ReadInt16()
            };
        }

        private SmdhApplicationTitle[] ReadTitles(BinaryReaderX reader, int count)
        {
            var result = new SmdhApplicationTitle[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadTitle(reader);

            return result;
        }

        private SmdhApplicationTitle ReadTitle(BinaryReaderX reader)
        {
            return new SmdhApplicationTitle
            {
                shortDesc = reader.ReadString(0x80),
                longDesc = reader.ReadString(0x100),
                publisher = reader.ReadString(0x80)
            };
        }

        private SmdhAppSettings ReadSettings(BinaryReaderX reader)
        {
            return new SmdhAppSettings
            {
                gameRating = reader.ReadBytes(0x10),
                regionLockout = reader.ReadInt32(),
                makerID = reader.ReadInt32(),
                makerBITID = reader.ReadInt64(),
                flags = reader.ReadInt32(),
                eulaVerMinor = reader.ReadByte(),
                eulaVerMajor = reader.ReadByte(),
                reserved = reader.ReadInt16(),
                animDefaultFrame = reader.ReadInt32(),
                streetPassID = reader.ReadInt32()
            };
        }

        private void WriteHeader(SmdhHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, Encoding.ASCII, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.reserved);
        }

        private void WriteTitles(IList<SmdhApplicationTitle> entries, BinaryWriterX writer)
        {
            foreach (SmdhApplicationTitle entry in entries)
                WriteTitle(entry, writer);
        }

        private void WriteTitle(SmdhApplicationTitle entry, BinaryWriterX writer)
        {
            writer.WriteString(entry.shortDesc, writeNullTerminator: false);
            writer.WriteString(entry.longDesc, writeNullTerminator: false);
            writer.WriteString(entry.publisher, writeNullTerminator: false);
        }

        private void WriteSettings(SmdhAppSettings settings, BinaryWriterX writer)
        {
            writer.Write(settings.gameRating);
            writer.Write(settings.regionLockout);
            writer.Write(settings.makerID);
            writer.Write(settings.makerBITID);
            writer.Write(settings.flags);
            writer.Write(settings.eulaVerMinor);
            writer.Write(settings.eulaVerMajor);
            writer.Write(settings.reserved);
            writer.Write(settings.animDefaultFrame);
            writer.Write(settings.streetPassID);
        }
    }
}
