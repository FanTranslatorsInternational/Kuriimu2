using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_yuusha_shisu.Images
{
    public class BTX
    {
        private const int HeaderSize = 0x30;

        private BtxHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Read name
            input.Position = _header.nameOffset;
            var name = br.ReadNullTerminatedString();

            // Read image data
            var dataLength = _header.width * _header.height * BtxSupport.GetBitDepth(_header.format) / 8;
            input.Position = _header.dataOffset;
            var imgData = br.ReadBytes(dataLength);

            // Read mip levels
            IList<byte[]> mips = new List<byte[]>();
            for (var i = 0; i < _header.mipLevels; i++)
            {
                dataLength = (_header.width << (i + 1)) * (_header.height << (i + 1)) * BtxSupport.GetBitDepth(_header.format) / 8;
                mips.Add(br.ReadBytes(dataLength));
            }

            // Read palette data
            var paletteLength = Math.Max(0, (int)input.Length - _header.paletteOffset);
            input.Position = _header.paletteOffset;
            var paletteData = br.ReadBytes(paletteLength);

            // Create image info
            var imageInfo = new ImageFileInfo
            {
                Name = name,
                BitDepth = !BtxSupport.ColorFormats.TryGetValue(_header.format, out var encoding)
                    ? BtxSupport.IndexFormats[_header.format].IndexEncoding.BitDepth
                    : encoding.BitDepth,
                ImageData = imgData,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height)
            };

            if (paletteLength > 0)
            {
                imageInfo.PaletteData = paletteData;
                imageInfo.PaletteFormat = 0;
            }

            if (_header.mipLevels > 0)
                imageInfo.MipMapData = mips;

            switch (_header.swizzleMode)
            {
                case 1:
                    imageInfo.RemapPixels = context => new VitaSwizzle(context);
                    break;
            }

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var nameOffset = HeaderSize;
            var dataOffset = (nameOffset + imageInfo.Name.Length + 0x10) & ~0xF;    // 0x10 = 0x1 + 0xF
            var paletteOffset = (dataOffset + imageInfo.ImageData.Length + (imageInfo.MipMapData?.Sum(x => x.Length) ?? 0) + 0x3F) & ~0x3F;

            // Write name
            output.Position = nameOffset;
            bw.WriteString(imageInfo.Name, Encoding.ASCII, false);

            // Write image data
            output.Position = dataOffset;
            bw.Write(imageInfo.ImageData);

            // Write mip levels
            if (imageInfo.MipMapData is not null)
                foreach (var mipData in imageInfo.MipMapData)
                    bw.Write(mipData);

            // Write palette data
            if (imageInfo.PaletteData is not null)
            {
                output.Position = paletteOffset;
                bw.Write(imageInfo.PaletteData);
            }

            // Update header
            _header.nameOffset = nameOffset;
            _header.dataOffset = dataOffset;
            _header.paletteOffset = paletteOffset;
            _header.mipLevels = (byte)(imageInfo.MipMapData?.Count ?? 0);
            _header.format = (byte)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private BtxHeader ReadHeader(BinaryReaderX reader)
        {
            return new BtxHeader
            {
                magic = reader.ReadString(4),
                clrCount = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                unk1 = reader.ReadInt32(),
                format = reader.ReadByte(),
                swizzleMode = reader.ReadByte(),
                mipLevels = reader.ReadByte(),
                unk2 = reader.ReadByte(),
                unk4 = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                unk5 = reader.ReadInt32(),
                paletteOffset = reader.ReadInt32(),
                nameOffset = reader.ReadInt32()
            };
        }

        private void WriteHeader(BtxHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.clrCount);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.unk1);
            writer.Write(header.format);
            writer.Write(header.swizzleMode);
            writer.Write(header.mipLevels);
            writer.Write(header.unk2);
            writer.Write(header.unk4);
            writer.Write(header.dataOffset);
            writer.Write(header.unk5);
            writer.Write(header.paletteOffset);
            writer.Write(header.nameOffset);
        }
    }
}
