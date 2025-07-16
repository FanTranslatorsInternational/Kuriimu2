using Komponent.IO;
using Komponent.Streams;
using plugin_level5.Common.Compression;
using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Image
{
    internal class Img00Reader : IImageReader
    {
        private readonly Decompressor _decompressor = new();

        public ImageRawData Read(Stream input)
        {
            using var br = new BinaryReaderX(input, false);

            // Header
            Img00Header header = ReadHeader(br);
            var formatVersion = new FormatVersion
            {
                Platform = GetPlatform(header),
                Version = GetVersion(header)
            };

            // Palette entries
            input.Position = header.paletteInfoOffset;
            Img00PaletteEntry[] paletteEntries = ReadPaletteEntries(br, header.paletteInfoCount);

            // Image entries
            input.Position = header.imageInfoOffset;
            Img00ImageEntry[] imageEntries = ReadImageEntries(br, header.imageInfoCount);

            // Get palette
            byte[]? paletteData = null;
            if (paletteEntries.Length > 0)
                paletteData = DecompressPalette(br, header, paletteEntries[0]);

            // Get image data
            Stream imageDataStream = DecompressImageData(br, header, imageEntries[0]);

            var images = new byte[header.imageCount][];

            byte[]? legacyData = null;

            if (formatVersion.Platform is PlatformType.Android && header.imageFormat is 0x2B)
            {
                images[0] = new byte[imageDataStream.Length];

                var readData = 0;
                while (readData < images[0].Length)
                    readData += imageDataStream.Read(images[0], readData, Math.Min(2048, images[0].Length - readData));
            }
            else
            {
                // Get tile table
                Stream tileDataStream = DecompressTiles(br, header, imageEntries[0]);

                // Combine tiles to full image data
                (byte[] imageData, legacyData) = CombineTiles(tileDataStream, imageDataStream, header.bitDepth);

                // Split image data and mip map data
                var dataOffset = 0;
                (int width, int height) = ((header.width + 7) & ~7, (header.height + 7) & ~7);
                for (var i = 0; i < header.imageCount; i++)
                {
                    images[i] = new byte[width * height * header.bitDepth / 8];
                    Array.Copy(imageData, dataOffset, images[i], 0, images[i].Length);

                    (width, height) = (width >> 1, height >> 1);
                    dataOffset += images[i].Length;
                }
            }

            var rawImageData = new ImageRawData
            {
                Version = formatVersion,

                BitDepth = header.bitDepth,
                Format = header.imageFormat,

                Width = header.width,
                Height = header.height,

                LegacyData = legacyData,

                Data = images[0],
                MipMapData = images.Length > 1 ? images[1..] : [],
                PaletteData = paletteData
            };

            if (paletteData != null)
            {
                rawImageData.PaletteBitDepth = paletteData.Length / paletteEntries[0].colorCount * 8;
                rawImageData.PaletteFormat = paletteEntries[0].format;
                rawImageData.PaletteData = paletteData;
            }

            return rawImageData;
        }

        private Img00Header ReadHeader(BinaryReaderX br)
        {
            return new Img00Header
            {
                magic = br.ReadString(8),
                entryOffset = br.ReadInt16(),
                imageFormat = br.ReadByte(),
                const1 = br.ReadByte(),
                imageCount = br.ReadByte(),
                bitDepth = br.ReadByte(),
                bytesPerTile = br.ReadInt16(),
                width = br.ReadInt16(),
                height = br.ReadInt16(),
                paletteInfoOffset = br.ReadUInt16(),
                paletteInfoCount = br.ReadUInt16(),
                imageInfoOffset = br.ReadUInt16(),
                imageInfoCount = br.ReadUInt16(),
                dataOffset = br.ReadInt32(),
                platform = br.ReadInt32()
            };
        }

        private Img00PaletteEntry[] ReadPaletteEntries(BinaryReaderX br, int count)
        {
            var result = new Img00PaletteEntry[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadPaletteEntry(br);
                br.BaseStream.Position += 4;
            }

            return result;
        }

        private Img00PaletteEntry ReadPaletteEntry(BinaryReaderX br)
        {
            return new Img00PaletteEntry
            {
                offset = br.ReadInt32(),
                size = br.ReadInt32(),
                colorCount = br.ReadInt16(),
                const0 = br.ReadByte(),
                format = br.ReadByte()
            };
        }

        private Img00ImageEntry[] ReadImageEntries(BinaryReaderX br, int count)
        {
            var result = new Img00ImageEntry[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadImageEntry(br);
                br.BaseStream.Position += 8;
            }

            return result;
        }

        private Img00ImageEntry ReadImageEntry(BinaryReaderX br)
        {
            return new Img00ImageEntry
            {
                tileOffset = br.ReadInt32(),
                tileSize = br.ReadInt32(),
                dataOffset = br.ReadInt32(),
                dataSize = br.ReadInt32()
            };
        }

        private byte[] DecompressPalette(BinaryReaderX br, Img00Header header, Img00PaletteEntry paletteEntry)
        {
            using Stream compressedPaletteStream = new SubStream(br.BaseStream, header.dataOffset + paletteEntry.offset, paletteEntry.size);
            using var output = new MemoryStream();

            _decompressor.Decompress(compressedPaletteStream, output, 0);

            return output.ToArray();
        }

        private Stream DecompressTiles(BinaryReaderX br, Img00Header header, Img00ImageEntry imageEntry)
        {
            using Stream compressedTileStream = new SubStream(br.BaseStream, header.dataOffset + imageEntry.tileOffset, imageEntry.tileSize);
            return _decompressor.Decompress(compressedTileStream, 0);
        }

        private Stream DecompressImageData(BinaryReaderX br, Img00Header header, Img00ImageEntry imageEntry)
        {
            using Stream compressedImageData = new SubStream(br.BaseStream, header.dataOffset + imageEntry.dataOffset, imageEntry.dataSize);
            return _decompressor.Decompress(compressedImageData, 0);
        }

        private (byte[], byte[]?) CombineTiles(Stream tileDataStream, Stream imageDataStream, int bitDepth)
        {
            using var tileDataReader = new BinaryReaderX(tileDataStream, true);

            int tileByteDepth = 64 * bitDepth / 8;
            long tileEntryCount = tileDataStream.Length / 2;
            var readEntry = new Func<BinaryReaderX, int>(br => br.ReadInt16());

            // Read legacy head
            byte[]? tileLegacy = null;

            ushort legacyIndicator = tileDataReader.ReadUInt16();
            tileDataReader.BaseStream.Position -= 2;

            if (legacyIndicator == 0x453)
            {
                tileLegacy = tileDataReader.ReadBytes(8);
                tileEntryCount = (tileDataStream.Length - 8) / 4;
                readEntry = br => br.ReadInt32();
            }

            var result = new byte[tileEntryCount * tileByteDepth];

            var tile = new byte[tileByteDepth];
            for (var i = 0; i < tileEntryCount; i++)
            {
                int entry = readEntry(tileDataReader);
                if (entry < 0)
                    continue;

                imageDataStream.Position = entry * tileByteDepth;
                _ = imageDataStream.Read(tile, 0, tileByteDepth);

                Array.Copy(tile, 0, result, i * tileByteDepth, tileByteDepth);
            }

            return (result, tileLegacy);
        }

        private PlatformType GetPlatform(Img00Header header)
        {
            switch (header.magic[3])
            {
                case 'C':
                    return PlatformType.Ctr;

                case 'P':
                    return PlatformType.Psp;

                case 'V':
                    return PlatformType.PsVita;

                case 'N':
                    return PlatformType.Switch;

                case 'A':
                    return PlatformType.Android;

                default:
                    throw new InvalidOperationException($"Unknown platform identifier '{header.magic[3]}' in image.");
            }
        }

        private int GetVersion(Img00Header header)
        {
            return int.Parse(header.magic[4..6]);
        }
    }
}
