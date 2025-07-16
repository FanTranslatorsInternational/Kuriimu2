using Kanvas.Swizzle;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Kryptography.Checksum.Crc;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class Bnr
    {
        private BnrHeader _header;
        private byte[] _titleInfo;
        private byte[] _animationInfo;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);
            br.SeekAlignment(0x20);

            // Read indices
            var indexData = br.ReadBytes(0x200);

            // Read palette
            var paletteData = br.ReadBytes(0x20);

            // Read title info
            _titleInfo = br.ReadBytes(GetTitleInfoSize(_header.version));

            // Read DSi animation info
            if (_header.version >= 0x103)
                _animationInfo = br.ReadBytes(0x1180);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = BnrSupport.GetEncodingDefinition().GetIndexEncoding(0).IndexEncoding.BitDepth,
                ImageData = indexData,
                ImageFormat = 0,
                ImageSize = new Size(32, 32),
                PaletteData = paletteData,
                PaletteFormat = 0,
                PaletteBitDepth = BnrSupport.GetEncodingDefinition().GetPaletteEncoding(0).BitDepth,
                RemapPixels = context => new NitroSwizzle(context)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var indexOffset = 0x20;
            var paletteOffset = indexOffset + 0x200;
            var titleInfoOffset = paletteOffset + 0x20;
            var animationInfoOffset = 0x1240;

            // Write animation info
            if (_header.version >= 0x103)
            {
                output.Position = animationInfoOffset;
                output.Write(_animationInfo);
            }

            // Write title info
            output.Position = titleInfoOffset;
            output.Write(_titleInfo);

            // Write palette
            output.Position = paletteOffset;
            output.Write(imageInfo.PaletteData);

            // Write index data
            output.Position = indexOffset;
            output.Write(imageInfo.ImageData);

            // Update header
            UpdateHeaderHashes(output);

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);

            bw.WriteAlignment(0x20);
        }

        private int GetTitleInfoSize(short version)
        {
            switch (version)
            {
                case 1:
                    return 0x600;

                case 2:
                    return 0x700;

                case 3:
                    return 0x800;

                default:
                    return 0x1000;
            }
        }

        private void UpdateHeaderHashes(Stream output)
        {
            var crc16 = Crc16.ModBus;

            var hashRegion = new SubStream(output, 0x20, 0x820);
            _header.crc16_v1 = crc16.ComputeValue(hashRegion);

            if (_header.version >= 2)
            {
                hashRegion = new SubStream(output, 0x20, 0x920);
                _header.crc16_v2 = crc16.ComputeValue(hashRegion);
            }

            if (_header.version >= 3)
            {
                hashRegion = new SubStream(output, 0x20, 0xA20);
                _header.crc16_v3 = crc16.ComputeValue(hashRegion);
            }

            if (_header.version >= 0x103)
            {
                hashRegion = new SubStream(output, 0x1240, 0x1180);
                _header.crc16_v103 = crc16.ComputeValue(hashRegion);
            }
        }

        private BnrHeader ReadHeader(BinaryReaderX reader)
        {
            return new BnrHeader
            {
                version = reader.ReadInt16(),
                crc16_v1 = reader.ReadUInt16(),
                crc16_v2 = reader.ReadUInt16(),
                crc16_v3 = reader.ReadUInt16(),
                crc16_v103 = reader.ReadUInt16()
            };
        }

        private void WriteHeader(BnrHeader header, BinaryWriterX writer)
        {
            writer.Write(header.version);
            writer.Write(header.crc16_v1);
            writer.Write(header.crc16_v2);
            writer.Write(header.crc16_v3);
            writer.Write(header.crc16_v103);
        }
    }
}
