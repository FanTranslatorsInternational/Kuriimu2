using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Configuration;
using Kanvas.Swizzle;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Image;
using Kryptography.Hash.Crc;

namespace plugin_level5.DS.Images
{
    class Limg
    {
        private static int _headerSize = Tools.MeasureType(typeof(LimgHeader));

        private static int _colorEntrySize = 0x2;
        private static int _tileEntrySize = 0x40;
        private static int _unk1EntrySize = 0x8;
        private static int _unk2EntrySize = 0x3;

        private LimgHeader _header;
        private byte[] _unkHeader;

        private byte[] _unkChunk1;
        private byte[] _unkChunk2;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Header
            _header = br.ReadType<LimgHeader>();
            _unkHeader = br.ReadBytes(0xC);

            // Palette data
            br.BaseStream.Position = _header.paletteOffset;
            var palette = br.ReadBytes(_header.colorCount * _colorEntrySize);

            // Get unknown tables
            br.BaseStream.Position = _header.unkOffset1;
            _unkChunk1 = br.ReadBytes(_header.unkCount1 * _unk1EntrySize);
            _unkChunk2 = br.ReadBytes(_header.unkCount2 * _unk2EntrySize);

            // Get tiles
            br.BaseStream.Position = _header.tileDataOffset;
            var tileIndices = br.ReadMultiple<short>(_header.tileEntryCount);

            // Inflate imageInfo
            var encoding = LimgSupport.LimgFormats[_header.imgFormat];
            var imageStream = new SubStream(input, _header.imageDataOffset, input.Length - _header.imageDataOffset);
            var imageData = CombineTiles(imageStream, tileIndices, encoding.Item1.BitsPerValue);

            return new ImageInfo
            {
                ImageFormat = _header.imgFormat,
                ImageData = imageData,
                ImageSize = new Size(_header.width, _header.height),
                Configuration = new ImageConfiguration()
                    .RemapPixelsWith(size => new NitroSwizzle(size.Width, size.Height)),

                PaletteData = palette,
                PaletteFormat = 0
            };
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);
            var encoding = LimgSupport.LimgFormats[imageInfo.ImageFormat];

            // Split into tiles
            var (tileIndices, imageStream) = SplitTiles(imageInfo.ImageData, encoding.Item1.BitsPerValue);

            // Write palette
            bw.BaseStream.Position = _headerSize + _unkHeader.Length;

            _header.paletteOffset = (uint)bw.BaseStream.Position;
            _header.colorCount = (short)(imageInfo.PaletteData.Length / _colorEntrySize);
            bw.Write(imageInfo.PaletteData);
            bw.WriteAlignment(4);

            // Write unknown tables
            _header.unkOffset1 = (short)bw.BaseStream.Position;
            _header.unkCount1 = (short)(_unkChunk1.Length / _unk1EntrySize);
            bw.Write(_unkChunk1);
            bw.WriteAlignment(4);

            _header.unkOffset2 = (short)bw.BaseStream.Position;
            _header.unkCount2 = (short)(_unkChunk2.Length / _unk2EntrySize);
            bw.Write(_unkChunk2);
            bw.WriteAlignment(4);

            // Write tiles
            _header.tileDataOffset = (short)bw.BaseStream.Position;
            _header.tileEntryCount = (short)tileIndices.Count;
            bw.WriteMultiple(tileIndices);
            bw.WriteAlignment(4);

            // Write imageInfo data
            _header.imageDataOffset = (short)bw.BaseStream.Position;
            _header.imageTileCount = (short)(imageStream.Length / _tileEntrySize);

            imageStream.Position = 0;
            imageStream.CopyTo(bw.BaseStream);
            bw.WriteAlignment(4);

            // Header
            bw.BaseStream.Position = 0;

            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;
            _header.paddedWidth = (short)((_header.width + 0xFF) & ~0xFF);
            _header.paddedHeight = (short)((_header.height + 0xFF) & ~0xFF);
            _header.imgFormat = (short)imageInfo.ImageFormat;

            bw.WriteType(_header);
            bw.Write(_unkHeader);
        }

        private byte[] CombineTiles(Stream imageData, IList<short> tileIndices, int bitDepth)
        {
            var tileSize = _tileEntrySize * bitDepth / 8;
            var result = new byte[tileIndices.Count * tileSize];

            var offset = 0;
            foreach (var tileIndex in tileIndices)
            {
                imageData.Position = tileIndex * tileSize;
                imageData.Read(result, offset, tileSize);

                offset += tileSize;
            }

            return result;
        }

        private (IList<short>, Stream) SplitTiles(byte[] imageData, int bitDepth)
        {
            var tileSize = _tileEntrySize * bitDepth / 8;

            var result = new MemoryStream();
            var tiles = new short[imageData.Length / tileSize];

            var tileDictionary = new Dictionary<uint, int>();
            var crc32 = Crc32.Create(Crc32Formula.Normal);

            var offset = 0;
            var tileIndex = 0;
            for (var i = 0; i < imageData.Length / tileSize; i++)
            {
                var tileStream = new SubStream(new MemoryStream(imageData), offset, tileSize);
                var hash = BinaryPrimitives.ReadUInt32BigEndian(crc32.Compute(tileStream));

                if (!tileDictionary.ContainsKey(hash))
                {
                    tileDictionary[hash] = tileIndex++;

                    tileStream.Position = 0;
                    tileStream.CopyTo(result);
                }

                tiles[i] = (short)tileDictionary[hash];
                offset += tileSize;
            }

            return (tiles, result);
        }
    }
}
