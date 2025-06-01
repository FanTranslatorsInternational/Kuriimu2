using Kanvas.Swizzle;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Kryptography.Checksum.Crc;
using SixLabors.ImageSharp;

namespace plugin_level5.NDS.Image
{
    public class Limg
    {
        private const int HeaderSize_ = 36;

        private const int ColorEntrySize_ = 0x2;
        private const int TileEntrySize_ = 0x40;
        private const int Unk1EntrySize_ = 0x8;
        private const int Unk2EntrySize_ = 0x3;

        private LimgHeader _header;
        private byte[] _unkHeader;

        private byte[] _unkChunk1;
        private byte[] _unkChunk2;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Header
            _header = ReadHeader(br);
            _unkHeader = br.ReadBytes(0xC);

            // Palette data
            br.BaseStream.Position = _header.paletteOffset;
            var palette = br.ReadBytes(_header.colorCount * ColorEntrySize_);

            // Get unknown tables
            br.BaseStream.Position = _header.unkOffset1;
            _unkChunk1 = br.ReadBytes(_header.unkCount1 * Unk1EntrySize_);
            _unkChunk2 = br.ReadBytes(_header.unkCount2 * Unk2EntrySize_);

            // Get tiles
            br.BaseStream.Position = _header.tileDataOffset;
            var tileIndices = ReadShorts(br, _header.tileEntryCount);

            // Inflate imageInfo
            var encoding = LimgSupport.LimgFormats[_header.imgFormat];
            var imageStream = new SubStream(input, _header.imageDataOffset, input.Length - _header.imageDataOffset);
            var imageData = CombineTiles(imageStream, tileIndices, encoding.Item1.BitDepth);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = encoding.Item1.BitDepth,
                ImageData = imageData,
                ImageFormat = _header.imgFormat,
                ImageSize = new Size(_header.width, _header.height),
                PaletteData = palette,
                PaletteFormat = 0,
                RemapPixels = context => new NitroSwizzle(context)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);
            var encoding = LimgSupport.LimgFormats[imageInfo.ImageFormat];

            // Split into tiles
            var (tileIndices, imageStream) = SplitTiles(imageInfo.ImageData, encoding.Item1.BitDepth);

            // Write palette
            bw.BaseStream.Position = HeaderSize_ + _unkHeader.Length;

            _header.paletteOffset = (uint)bw.BaseStream.Position;
            _header.colorCount = (short)(imageInfo.PaletteData.Length / ColorEntrySize_);
            bw.Write(imageInfo.PaletteData);
            bw.WriteAlignment(4);

            // Write unknown tables
            _header.unkOffset1 = (short)bw.BaseStream.Position;
            _header.unkCount1 = (short)(_unkChunk1.Length / Unk1EntrySize_);
            bw.Write(_unkChunk1);
            bw.WriteAlignment(4);

            _header.unkOffset2 = (short)bw.BaseStream.Position;
            _header.unkCount2 = (short)(_unkChunk2.Length / Unk2EntrySize_);
            bw.Write(_unkChunk2);
            bw.WriteAlignment(4);

            // Write tiles
            _header.tileDataOffset = (short)bw.BaseStream.Position;
            _header.tileEntryCount = (short)tileIndices.Count;
            WriteShorts(tileIndices, bw);
            bw.WriteAlignment(4);

            // Write imageInfo data
            _header.imageDataOffset = (short)bw.BaseStream.Position;
            _header.imageTileCount = (short)(imageStream.Length / TileEntrySize_);

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

            WriteHeader(_header, bw);
            bw.Write(_unkHeader);
        }

        private byte[] CombineTiles(Stream imageData, IList<short> tileIndices, int bitDepth)
        {
            var tileSize = TileEntrySize_ * bitDepth / 8;
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
            var tileSize = TileEntrySize_ * bitDepth / 8;

            var result = new MemoryStream();
            var tiles = new short[imageData.Length / tileSize];

            var tileDictionary = new Dictionary<uint, int>();
            var crc32 = Crc32.Crc32B;

            var offset = 0;
            var tileIndex = 0;
            for (var i = 0; i < imageData.Length / tileSize; i++)
            {
                var tileStream = new SubStream(new MemoryStream(imageData), offset, tileSize);
                var hash = crc32.ComputeValue(tileStream);

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

        private LimgHeader ReadHeader(BinaryReaderX reader)
        {
            return new LimgHeader
            {
                magic = reader.ReadString(4),
                paletteOffset = reader.ReadUInt32(),
                unkOffset1 = reader.ReadInt16(),
                unkCount1 = reader.ReadInt16(),
                unkOffset2 = reader.ReadInt16(),
                unkCount2 = reader.ReadInt16(),
                tileDataOffset = reader.ReadInt16(),
                tileEntryCount = reader.ReadInt16(),
                imageDataOffset = reader.ReadInt16(),
                imageTileCount = reader.ReadInt16(),
                imgFormat = reader.ReadInt16(),
                colorCount = reader.ReadInt16(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                paddedWidth = reader.ReadInt16(),
                paddedHeight = reader.ReadInt16()
            };
        }

        private short[] ReadShorts(BinaryReaderX reader, int count)
        {
            var result = new short[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt16();

            return result;
        }

        private void WriteHeader(LimgHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.paletteOffset);
            writer.Write(header.unkOffset1);
            writer.Write(header.unkCount1);
            writer.Write(header.unkOffset2);
            writer.Write(header.unkCount2);
            writer.Write(header.tileDataOffset);
            writer.Write(header.tileEntryCount);
            writer.Write(header.imageDataOffset);
            writer.Write(header.imageTileCount);
            writer.Write(header.imgFormat);
            writer.Write(header.colorCount);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.paddedWidth);
            writer.Write(header.paddedHeight);
        }

        private void WriteShorts(IList<short> entries, BinaryWriterX writer)
        {
            foreach (short entry in entries)
                writer.Write(entry);
        }
    }
}
