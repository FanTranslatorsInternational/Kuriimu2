using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Image;
using Kryptography.Hash.Crc;

namespace plugin_level5.DS.Images
{
    class Gtxt
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(GtxtLtHeader));

        private GtxtLtHeader _ltHeader;
        private GtxtLpHeader _lpHeader;

        private byte[] _unkRegion1;
        private byte[] _unkRegion2;

        public ImageInfo Load(Stream ltInput, Stream lpInput)
        {
            using var ltBr = new BinaryReaderX(ltInput);
            using var lpBr = new BinaryReaderX(lpInput);

            // Read headers
            _ltHeader = ltBr.ReadType<GtxtLtHeader>();
            _lpHeader = lpBr.ReadType<GtxtLpHeader>();

            // Read unknown regions
            _unkRegion1 = ltBr.ReadBytes(_ltHeader.unkCount1 * 8);
            _unkRegion2 = ltBr.ReadBytes(_ltHeader.unkCount2 * 3);

            // Read palette data
            var paletteData = lpBr.ReadBytes(_lpHeader.colorCount * (GtxtSupport.PaletteFormats[_lpHeader.paletteFormat].BitDepth / 8));

            // Combine tiles
            var tileBitDepth = GtxtSupport.IndexFormats[_ltHeader.indexFormat].BitDepth;
            var tileByteDepth = 64 * tileBitDepth / 8;

            var tileIndexStream = new SubStream(ltInput, _ltHeader.indexOffset, _ltHeader.indexCount * 2);
            var tileStream = new SubStream(ltInput, _ltHeader.dataOffset, _ltHeader.tileCount * tileByteDepth);

            var imageData = CombineTiles(tileIndexStream, tileStream, tileBitDepth);

            // Create image info
            var imageInfo = new ImageInfo(imageData, _ltHeader.indexFormat, new Size(_ltHeader.width, _ltHeader.height))
            {
                PaletteData = paletteData,
                PaletteFormat = _lpHeader.paletteFormat
            };
            imageInfo.RemapPixels.With(context => new NitroSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream ltOutput, Stream lpOutput, ImageInfo imageInfo)
        {
            WritePaletteFile(lpOutput, imageInfo);
            WriteImageFile(ltOutput, imageInfo);
        }

        private void WritePaletteFile(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Update header
            _lpHeader.paletteFormat = (short)imageInfo.PaletteFormat;
            _lpHeader.colorCount = (short)(imageInfo.PaletteData.Length /
                                           (GtxtSupport.PaletteFormats[imageInfo.PaletteFormat].BitDepth / 8));

            // Write palette header
            bw.WriteType(_lpHeader);

            // Write palette data
            bw.Write(imageInfo.PaletteData);
        }

        private void WriteImageFile(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var unkRegion1Offset = HeaderSize;
            var unkRegion2Offset = unkRegion1Offset + _unkRegion1.Length;
            var tileIndexOffset = unkRegion2Offset + _unkRegion2.Length;

            // Split tiles
            var tileBitDepth = GtxtSupport.IndexFormats[_ltHeader.indexFormat].BitDepth;
            var (imageData, tileData) = SplitTiles(new MemoryStream(imageInfo.ImageData), tileBitDepth);

            var imageDataOffset = tileIndexOffset + tileData.Length;

            // Write image data
            output.Position = imageDataOffset;
            imageData.CopyTo(output);

            // Write tile data
            output.Position = tileIndexOffset;
            tileData.CopyTo(output);

            // Write unknown regions
            output.Position = unkRegion1Offset;
            bw.Write(_unkRegion1);
            bw.Write(_unkRegion2);

            // Update header
            _ltHeader.indexFormat = (byte)imageInfo.ImageFormat;

            _ltHeader.width = (short)imageInfo.ImageSize.Width;
            _ltHeader.height = (short)imageInfo.ImageSize.Height;
            _ltHeader.paddedWidth = (short)GtxtSupport.ToPowerOfTwo(imageInfo.ImageSize.Width);
            _ltHeader.paddedHeight = (short)((imageInfo.ImageSize.Height + 7) & ~7);

            _ltHeader.unkOffset1 = (short)unkRegion1Offset;
            _ltHeader.unkOffset2 = (short)unkRegion2Offset;
            _ltHeader.indexOffset = (short)tileIndexOffset;
            _ltHeader.dataOffset = (short)imageDataOffset;

            _ltHeader.unkCount1 = (short)(_unkRegion1.Length / 8);
            _ltHeader.unkCount2 = (short)(_unkRegion2.Length / 3);
            _ltHeader.indexCount = (short)(tileData.Length / 2);
            _ltHeader.tileCount = (short)(imageData.Length / (64 * tileBitDepth / 8));

            // Write header
            output.Position = 0;
            bw.WriteType(_ltHeader);
        }

        private byte[] CombineTiles(Stream tileTableStream, Stream imageDataStream, int bitDepth)
        {
            using var tileTable = new BinaryReaderX(tileTableStream);

            var tileByteDepth = 64 * bitDepth / 8;
            var tile = new byte[tileByteDepth];

            var result = new MemoryStream();
            while (tileTableStream.Position < tileTableStream.Length)
            {
                var entry = tileTable.ReadInt16();
                if (entry == -1)
                {
                    Array.Clear(tile, 0, tile.Length);
                }
                else
                {
                    imageDataStream.Position = entry * tileByteDepth;
                    imageDataStream.Read(tile, 0, tile.Length);
                }

                result.Write(tile, 0, tile.Length);
            }

            result.Position = 0;
            return result.ToArray();
        }

        private (Stream imageData, Stream tileData) SplitTiles(Stream image, int bitDepth)
        {
            var tileByteDepth = 64 * bitDepth / 8;

            var tileTable = new MemoryStream();
            var imageData = new MemoryStream();
            using var tileBw = new BinaryWriterX(tileTable, true);
            using var imageBw = new BinaryWriterX(imageData, true);

            var crc32 = Crc32.Default;
            var tileDictionary = new Dictionary<uint, short>();

            // Add placeholder tile for all 0's
            var zeroTileHash = crc32.ComputeValue(new byte[tileByteDepth]);
            tileDictionary[zeroTileHash] = -1;

            var imageOffset = 0;
            var tileIndex = 0;
            while (imageOffset < image.Length)
            {
                var tile = new SubStream(image, imageOffset, tileByteDepth);
                var tileHash = crc32.ComputeValue(tile);
                if (!tileDictionary.ContainsKey(tileHash))
                {
                    tile.Position = 0;
                    tile.CopyTo(imageBw.BaseStream);

                    tileDictionary[tileHash] = (short)tileIndex++;
                }

                tileBw.Write(tileDictionary[tileHash]);

                imageOffset += tileByteDepth;
            }

            imageData.Position = tileTable.Position = 0;
            return (imageData, tileTable);
        }
    }
}
