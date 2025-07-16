using System.Text;
using Komponent.IO;
using Kryptography.Checksum.Crc;
using plugin_level5.Common.Compression;
using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Image
{
    internal class Img00Writer : IImageWriter
    {
        private readonly Compressor _compressor = new();
        private readonly Crc32 _crc32 = Crc32.Crc32B;

        public void Write(ImageRawData data, Stream output)
        {
            (int paddedWidth, int paddedHeight) = ((data.Width + 7) & ~7, (data.Height + 7) & ~7);
            int bitDepth = data.Data.Length * 8 / (paddedWidth * paddedHeight);

            // Compress palette data
            Stream? paletteStream = null;
            if (data.PaletteData != null)
                paletteStream = _compressor.Compress(new MemoryStream(data.PaletteData), Level5CompressionMethod.Huffman4Bit);

            // Create tile and image data
            Stream dataStream;
            Stream? tileStream = null;

            if (data.Version.Platform is PlatformType.Android && data.Format is 0x2B)
            {
                dataStream = new MemoryStream(data.Data);
            }
            else
            {
                (dataStream, tileStream) = SplitTiles(data.Data, data.LegacyData, bitDepth);

                // Compress tile data
                tileStream = _compressor.Compress(tileStream, Level5CompressionMethod.Huffman4Bit);
            }

            // Compress image data
            dataStream = _compressor.Compress(dataStream, Level5CompressionMethod.Lz10);

            // Create palette entries
            Img00PaletteEntry[] paletteEntries = CreatePaletteEntries(data, paletteStream);

            // Create image entries
            Img00ImageEntry[] imageEntries = CreateImageEntries(paletteEntries, tileStream, dataStream);

            // Create header
            Img00Header header = CreateHeader(data, paletteEntries, imageEntries);

            // Write header data
            WriteHeaderEntries(header, paletteEntries, imageEntries, output);

            // Write data
            WriteData(header, paletteStream, tileStream, dataStream, output);
        }

        private (Stream imageData, Stream tileData) SplitTiles(byte[] imageData, byte[]? legacyData, int bitDepth)
        {
            int tileByteDepth = 64 * bitDepth / 8;

            var tileStream = new MemoryStream();
            var dataStream = new MemoryStream();

            using var tileWriter = new BinaryWriterX(tileStream, true);
            using var dataWriter = new BinaryWriterX(dataStream, true);

            if (legacyData != null)
                tileStream.Write(legacyData, 0, legacyData.Length);

            var tileDictionary = new Dictionary<uint, int>();

            // Add placeholder tile for all 0's
            uint zeroTileHash = _crc32.ComputeValue(new byte[tileByteDepth]);
            tileDictionary[zeroTileHash] = -1;

            var imageOffset = 0;
            var tileIndex = 0;
            while (imageOffset < imageData.Length)
            {
                Span<byte> tileData = imageData.AsSpan(imageOffset, tileByteDepth);

                uint tileHash = _crc32.ComputeValue(tileData);
                if (!tileDictionary.ContainsKey(tileHash))
                {
                    dataWriter.Write(tileData);

                    tileDictionary[tileHash] = tileIndex++;
                }

                if (legacyData != null)
                    tileWriter.Write(tileDictionary[tileHash]);
                else
                    tileWriter.Write((short)tileDictionary[tileHash]);

                imageOffset += tileByteDepth;
            }

            dataStream.Position = tileStream.Position = 0;
            return (dataStream, tileStream);
        }

        private Img00PaletteEntry[] CreatePaletteEntries(ImageRawData imageData, Stream? paletteStream)
        {
            if (imageData.PaletteFormat < 0 || imageData.PaletteData == null || paletteStream == null)
                return [];

            return
            [
                new Img00PaletteEntry
                {
                    offset = 0,
                    size = (int)paletteStream.Length,
                    colorCount = (short)(imageData.PaletteData.Length * 8 / imageData.PaletteBitDepth),
                    const0 = 1,
                    format = (byte)imageData.PaletteFormat
                }
            ];
        }

        private Img00ImageEntry[] CreateImageEntries(Img00PaletteEntry[] paletteEntries, Stream? tileStream, Stream imageStream)
        {
            int tileOffset = paletteEntries.Length <= 0 ? 0 : paletteEntries[^1].offset + paletteEntries[^1].size;
            tileOffset = (tileOffset + 3) & ~3;

            return
            [
                new Img00ImageEntry
                {
                    tileOffset = tileStream is null ? 0 : tileOffset,
                    tileSize = tileStream is null ? 0 : (int)tileStream.Length,
                    dataOffset = tileStream is null ? 0 : (int)((tileOffset + tileStream.Length + 3) & ~3),
                    dataSize = (int)imageStream.Length
                }
            ];
        }

        private Img00Header CreateHeader(ImageRawData imageData, Img00PaletteEntry[] paletteEntries, Img00ImageEntry[] imageEntries)
        {
            return new Img00Header
            {
                magic = GetMagic(imageData),
                entryOffset = 0x30,
                imageFormat = (byte)imageData.Format,
                const1 = 1,
                imageCount = (byte)(imageData.MipMapData.Length + 1),
                bitDepth = (byte)imageData.BitDepth,
                bytesPerTile = (short)(64 * imageData.BitDepth / 8),
                width = (short)imageData.Width,
                height = (short)imageData.Height,
                paletteInfoOffset = 0x30,
                paletteInfoCount = (ushort)paletteEntries.Length,
                imageInfoOffset = (ushort)(0x30 + paletteEntries.Length * 0x10),
                imageInfoCount = (ushort)imageEntries.Length,
                dataOffset = (ushort)(0x30 + paletteEntries.Length * 0x10 + imageEntries.Length * 0x18),
                platform = GetPlatformVersion(imageData.Version.Platform)
            };
        }

        private void WriteHeaderEntries(Img00Header header, Img00PaletteEntry[] paletteEntries, Img00ImageEntry[] imageEntries, Stream output)
        {
            output.Position = 0;
            WriteHeader(header, output);

            output.Position = header.paletteInfoOffset;
            WritePaletteEntries(paletteEntries, output);

            output.Position = header.imageInfoOffset;
            WriteImageEntries(imageEntries, output);
        }

        private void WriteHeader(Img00Header header, Stream output)
        {
            using var writer = new BinaryWriterX(output, true);

            writer.WriteString(header.magic, Encoding.ASCII, false, false);
            writer.Write(header.entryOffset);
            writer.Write(header.imageFormat);
            writer.Write(header.const1);
            writer.Write(header.imageCount);
            writer.Write(header.bitDepth);
            writer.Write(header.bytesPerTile);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.paletteInfoOffset);
            writer.Write(header.paletteInfoCount);
            writer.Write(header.imageInfoOffset);
            writer.Write(header.imageInfoCount);
            writer.Write(header.dataOffset);
            writer.Write(header.platform);
        }

        private void WritePaletteEntries(Img00PaletteEntry[] paletteEntries, Stream output)
        {
            foreach (Img00PaletteEntry paletteEntry in paletteEntries)
            {
                WritePaletteEntry(paletteEntry, output);
                output.Position += 4;
            }
        }

        private void WritePaletteEntry(Img00PaletteEntry paletteEntry, Stream output)
        {
            using var writer = new BinaryWriterX(output, true);

            writer.Write(paletteEntry.offset);
            writer.Write(paletteEntry.size);
            writer.Write(paletteEntry.colorCount);
            writer.Write(paletteEntry.const0);
            writer.Write(paletteEntry.format);
        }

        private void WriteImageEntries(Img00ImageEntry[] imageEntries, Stream output)
        {
            foreach (Img00ImageEntry imageEntry in imageEntries)
            {
                WriteImageEntry(imageEntry, output);
                output.Position += 8;
            }
        }

        private void WriteImageEntry(Img00ImageEntry imageEntry, Stream output)
        {
            using var writer = new BinaryWriterX(output, true);

            writer.Write(imageEntry.tileOffset);
            writer.Write(imageEntry.tileSize);
            writer.Write(imageEntry.dataOffset);
            writer.Write(imageEntry.dataSize);
        }

        private void WriteData(Img00Header header, Stream? paletteStream, Stream? tileStream, Stream dataStream, Stream output)
        {
            using var writer = new BinaryWriterX(output, true);

            output.Position = header.dataOffset;

            if (paletteStream != null)
            {
                paletteStream.CopyTo(output);
                writer.WriteAlignment(4);
            }

            if (tileStream != null)
            {
                tileStream.CopyTo(output);
                writer.WriteAlignment(4);
            }

            dataStream.CopyTo(output);
            writer.WriteAlignment(4);
        }

        private string GetMagic(ImageRawData image)
        {
            char identifier = GetPlatformIdentifier(image.Version.Platform);
            string version = GetPlatformIdentifier(image.Version.Version);

            return $"IMG{identifier}{version}\0\0";
        }

        private int GetPlatformVersion(PlatformType platform)
        {
            switch (platform)
            {
                case PlatformType.Ctr:
                    return 3;

                case PlatformType.Psp:
                    return 1;

                case PlatformType.PsVita:
                case PlatformType.Android:
                case PlatformType.Switch:
                    return 0;

                default:
                    throw new InvalidOperationException($"Unknown platform {platform} for image.");
            }
        }

        private char GetPlatformIdentifier(PlatformType platform)
        {
            switch (platform)
            {
                case PlatformType.Ctr:
                    return 'C';

                case PlatformType.Psp:
                    return 'P';

                case PlatformType.PsVita:
                    return 'V';

                case PlatformType.Android:
                    return 'A';

                case PlatformType.Switch:
                    return 'N';

                default:
                    throw new InvalidOperationException($"Unknown platform {platform} for image.");
            }
        }

        private string GetPlatformIdentifier(int version)
        {
            return $"{version:00}";
        }
    }
}
