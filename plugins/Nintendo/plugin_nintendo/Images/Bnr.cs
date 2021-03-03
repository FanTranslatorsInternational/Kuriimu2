using System.Buffers.Binary;
using System.Drawing;
using System.IO;
using System.Text;
using Kanvas.Configuration;
using Kanvas.Swizzle;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Image;
using Kryptography.Hash.Crc;

namespace plugin_nintendo.Images
{
    class Bnr
    {
        private BnrHeader _header;
        private byte[] _titleInfo;
        private byte[] _animationInfo;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<BnrHeader>();

            // Read indices
            var indexData = br.ReadBytes(0x200);

            // Read palette
            var paletteData = br.ReadBytes(0x20);

            // Read title info
            _titleInfo = br.ReadBytes(GetTitleInfoSize(_header.version));

            // Read DSi animation info
            if (_header.version >= 0x103)
                _animationInfo = br.ReadBytes(0x1180);

            var imageInfo =new ImageInfo(indexData, 0, new Size(32, 32))
            {
                PaletteData = paletteData,
                PaletteFormat = 0
            };
            imageInfo.RemapPixels.With(context => new NitroSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
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

            // Write padding
            output.Position = output.Length;
            bw.WriteAlignment(0x200, 0xFF);

            // Update header
            UpdateHeaderHashes(output);

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
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
            _header.crc16_v1 = BinaryPrimitives.ReadUInt16BigEndian(crc16.Compute(hashRegion));

            if (_header.version >= 2)
            {
                hashRegion = new SubStream(output, 0x20, 0x920);
                _header.crc16_v2 = BinaryPrimitives.ReadUInt16BigEndian(crc16.Compute(hashRegion));
            }

            if (_header.version >= 3)
            {
                hashRegion = new SubStream(output, 0x20, 0xA20);
                _header.crc16_v3 = BinaryPrimitives.ReadUInt16BigEndian(crc16.Compute(hashRegion));
            }

            if (_header.version >= 0x103)
            {
                hashRegion = new SubStream(output, 0x1240, 0x1180);
                _header.crc16_v103 = BinaryPrimitives.ReadUInt16BigEndian(crc16.Compute(hashRegion));
            }
        }
    }
}
