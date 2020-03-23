#if NET_CORE_31
using System.Buffers.Binary;
#endif

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Configuration;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Image;
using Kryptography.Hash.Crc;
using plugin_level5.Compression;

namespace plugin_level5.Images
{
    class Imgc
    {
        private ImgcHeader _header;
        private int _headerSize = Tools.MeasureType(typeof(ImgcHeader));

        private byte[] _tileTableLegacyStart;

        private Level5CompressionMethod _tileTableCompression;
        private Level5CompressionMethod _imageDataCompression;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Header
            _header = br.ReadType<ImgcHeader>();
            if (_header.imageFormat == 28 && _header.bitDepth == 8)
                _header.imageFormat = 29;

            // Get tile table
            var tileTableComp = new SubStream(input, _header.tableDataOffset, _header.tileTableSize);
            var tileTable = new MemoryStream();
            Level5Compressor.Decompress(tileTableComp, tileTable);

            tileTableComp.Position = 0;
            _tileTableCompression = (Level5CompressionMethod)(tileTableComp.ReadByte() & 0x7);

            // Get image data
            var imageDataComp = new SubStream(input, _header.tableDataOffset + _header.tileTableSizePadded,
                _header.imgDataSize);
            var imageData = new MemoryStream();
            Level5Compressor.Decompress(imageDataComp, imageData);

            imageDataComp.Position = 0;
            _imageDataCompression = (Level5CompressionMethod)(imageDataComp.ReadByte() & 0x7);

            // Combine tiles to full image
            tileTable.Position = imageData.Position = 0;
            var combinedImage = CombineTiles(tileTable, imageData, _header.bitDepth);

            return new ImageInfo
            {
                ImageData = combinedImage,
                ImageFormat = _header.imageFormat,
                ImageSize = new Size(_header.width, _header.height),
                Configuration = new ImageConfiguration()
                    .RemapPixelsWith(size => new ImgcSwizzle(size.Width, size.Height))
            };
        }

        public void Save(Stream output, ImageInfo image)
        {
            using var bw = new BinaryWriterX(output);

            // Header
            _header.width = (short)image.ImageSize.Width;
            _header.height = (short)image.ImageSize.Height;
            _header.imageFormat = (byte)(image.ImageFormat == 29 ? 28 : image.ImageFormat);
            _header.bitDepth = (byte)(image.ImageFormat == 29 ? 8 :
                ImgcSupport.ImgcFormats[image.ImageFormat].BitDepth / ImgcSupport.ImgcFormats[image.ImageFormat].ColorsPerValue);

            // Create reduced tiles and indices
            var (imageData, tileTable) = SplitTiles(new MemoryStream(image.ImageData));

            // Write tile table
            output.Position = _headerSize;
            Level5Compressor.Compress(tileTable, output, _tileTableCompression);

            _header.tileTableSize = (int)output.Length - _headerSize;
            _header.tileTableSizePadded = (_header.tileTableSize + 3) & ~3;

            // Write image tiles
            output.Position = _headerSize + _header.tileTableSizePadded;
            Level5Compressor.Compress(imageData, output, _imageDataCompression);
            bw.WriteAlignment(4);

            _header.imgDataSize = (int)(output.Length - (_headerSize + _header.tileTableSizePadded));

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private byte[] CombineTiles(Stream tileTableStream, Stream imageDataStream, int bitDepth)
        {
            using var tileTable = new BinaryReaderX(tileTableStream);

            var tileByteDepth = 64 * bitDepth / 8;

            var entryLength = 2;

            var legacyIndicator = tileTable.ReadInt16();
            tileTable.BaseStream.Position -= 2;
            if (legacyIndicator == 0x453)
            {
                _tileTableLegacyStart = tileTable.ReadBytes(8);

                entryLength = 4;
            }

            var result = new MemoryStream();
            while (tileTableStream.Position < tileTableStream.Length)
            {
                var entry = entryLength == 2 ? tileTable.ReadInt16() : tileTable.ReadInt32();
                if (entry == -1)
                {
                    var tile = new byte[tileByteDepth];
                    result.Write(tile, 0, tile.Length);
                }
                else
                {
                    var tile = new byte[tileByteDepth];
                    imageDataStream.Position = entry * tileByteDepth;
                    imageDataStream.Read(tile, 0, tile.Length);
                    result.Write(tile, 0, tile.Length);
                }
            }

            return result.ToArray();
        }

        private (Stream imageData, Stream tileData) SplitTiles(Stream image)
        {
            var bitDepth = ImgcSupport.ImgcFormats[_header.imageFormat].BitDepth / ImgcSupport.ImgcFormats[_header.imageFormat].ColorsPerValue;
            var tileByteDepth = 64 * bitDepth / 8;

            var tileTable = new MemoryStream();
            var imageData = new MemoryStream();
            using var tileBw = new BinaryWriterX(tileTable, true);
            using var imageBw = new BinaryWriterX(imageData, true);

            if (_tileTableLegacyStart != null)
                tileTable.Write(_tileTableLegacyStart, 0, _tileTableLegacyStart.Length);

            var crc32 = Crc32.Create(Crc32Formula.Normal);
            var tileDictionary = new Dictionary<uint, int>();

            // Add placeholder tile for all 0's
            var zeroTileHash = ToUInt32BigEndian(crc32.Compute(new byte[tileByteDepth]));
            tileDictionary[zeroTileHash] = -1;

            var imageOffset = 0;
            var tileIndex = 0;
            while (imageOffset < image.Length)
            {
                var tile = new SubStream(image, imageOffset, tileByteDepth);
                var tileHash = ToUInt32BigEndian(crc32.Compute(tile));
                if (!tileDictionary.ContainsKey(tileHash))
                {
                    tile.Position = 0;
                    tile.CopyTo(imageBw.BaseStream);

                    tileDictionary[tileHash] = tileIndex++;
                }

                if (_tileTableLegacyStart != null)
                    tileBw.Write(tileDictionary[tileHash]);
                else
                    tileBw.Write((short)tileDictionary[tileHash]);

                imageOffset += tileByteDepth;
            }

            imageData.Position = tileTable.Position = 0;
            return (imageData, tileTable);
        }

        // TODO: Remove when only net core
        private uint ToUInt32BigEndian(byte[] input)
        {
#if NET_CORE_31
            return BinaryPrimitives.ReadUInt32BigEndian(input);
#else
            return (uint)((input[0] << 24) | (input[1] << 16) | (input[2] << 8) | input[3]);
#endif
        }
    }
}
