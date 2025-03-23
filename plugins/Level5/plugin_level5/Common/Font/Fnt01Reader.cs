using Komponent.IO;
using Komponent.Streams;
using plugin_level5.Common.Compression;
using plugin_level5.Common.Font.Models;

namespace plugin_level5.Common.Font
{
    public class Fnt01Reader: IFontReader
    {
        private readonly Decompressor _decompressor = new();

        public FontData Read(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            Fnt01Header header = ReadHeader(br);

            Fnt01CharSize[] charactersSizes = ReadCharSizes(br, header);
            Fnt01CharInfo[] largeCharacterInfos = ReadCharInfos(br, header.largeCharOffset << 2, header.largeCharCount);
            Fnt01CharInfo[] smallCharacterInfos = ReadCharInfos(br, header.smallCharOffset << 2, header.smallCharCount);

            IDictionary<char, FontGlyphData> largeGlyphs = CreateGlyphs(charactersSizes, largeCharacterInfos);
            IDictionary<char, FontGlyphData> smallGlyphs = CreateGlyphs(charactersSizes, smallCharacterInfos);

            return CreateFontData(header, largeGlyphs, smallGlyphs);
        }

        private Fnt01Header ReadHeader(BinaryReaderX br)
        {
            return new Fnt01Header
            {
                magic = br.ReadString(8),
                version = br.ReadInt32(),
                largeCharHeight = br.ReadInt16(),
                smallCharHeight = br.ReadInt16(),
                largeEscapeCharacterIndex = br.ReadUInt16(),
                smallEscapeCharacterIndex = br.ReadUInt16(),
                zero0 = br.ReadInt64(),

                charSizeOffset = br.ReadInt16(),
                charSizeCount = br.ReadInt16(),
                largeCharOffset = br.ReadInt16(),
                largeCharCount = br.ReadInt16(),
                smallCharOffset = br.ReadInt16(),
                smallCharCount = br.ReadInt16(),
            };
        }

        private Fnt01CharSize[] ReadCharSizes(BinaryReaderX br, Fnt01Header header)
        {
            using Stream compressedCharSizes = new SubStream(br.BaseStream, header.charSizeOffset << 2);
            using Stream decompressedCharSizes = _decompressor.Decompress(compressedCharSizes, 0);

            using var sizeReader = new BinaryReaderX(decompressedCharSizes);

            var result = new Fnt01CharSize[header.charSizeCount];
            for (var i = 0; i < header.charSizeCount; i++)
                result[i] = ReadCharSize(sizeReader);

            return result;
        }

        private Fnt01CharSize ReadCharSize(BinaryReaderX br)
        {
            return new Fnt01CharSize
            {
                offsetX = br.ReadSByte(),
                offsetY = br.ReadSByte(),
                glyphWidth = br.ReadByte(),
                glyphHeight = br.ReadByte()
            };
        }

        private Fnt01CharInfo[] ReadCharInfos(BinaryReaderX br, int charInfoOffset, int charInfoCount)
        {
            using Stream compressedCharInfos = new SubStream(br.BaseStream, charInfoOffset);
            using Stream decompressedCharInfos = _decompressor.Decompress(compressedCharInfos, 0);

            using var infoReader = new BinaryReaderX(decompressedCharInfos);

            var result = new Fnt01CharInfo[charInfoCount];
            for (var i = 0; i < charInfoCount; i++)
                result[i] = ReadCharInfo(infoReader);

            return result;
        }

        private Fnt01CharInfo ReadCharInfo(BinaryReaderX br)
        {
            return new Fnt01CharInfo
            {
                charCode = br.ReadUInt16(),
                charSizeInfo = br.ReadUInt16(),
                imageInfo = br.ReadUInt32()
            };
        }

        private IDictionary<char, FontGlyphData> CreateGlyphs(Fnt01CharSize[] characterSizes, Fnt01CharInfo[] characterInfos)
        {
            var result = new Dictionary<char, FontGlyphData>();
            for (var i = 0; i < characterInfos.Length; i++)
                result[(char)characterInfos[i].charCode] = CreateGlyph(characterSizes[characterInfos[i].charSizeInfo & 0x3FF], characterInfos[i]);

            return result;
        }

        private FontGlyphData CreateGlyph(Fnt01CharSize characterSize, Fnt01CharInfo characterInfo)
        {
            return new FontGlyphData
            {
                CodePoint = characterInfo.charCode,
                Width = characterInfo.charSizeInfo >> 10,
                Location = new FontGlyphLocationData
                {
                    Y = (int)(characterInfo.imageInfo >> 18),
                    X = (int)((characterInfo.imageInfo >> 4) & 0x3FFF),
                    Index = (int)(characterInfo.imageInfo & 0xF)
                },
                Description = new FontGlyphDescriptionData
                {
                    X = characterSize.offsetX,
                    Y = characterSize.offsetY,
                    Width = characterSize.glyphWidth,
                    Height = characterSize.glyphHeight
                }
            };
        }

        private FontData CreateFontData(Fnt01Header header, IDictionary<char, FontGlyphData> largeGlyphs, IDictionary<char, FontGlyphData> smallGlyphs)
        {
            return new FontData
            {
                Version = new FormatVersion
                {
                    Platform = GetPlatform(header),
                    Version = GetVersion(header)
                },
                LargeFont = new FontGlyphsData
                {
                    FallbackCharacter = largeGlyphs.Keys.ElementAtOrDefault(header.largeEscapeCharacterIndex),
                    MaxHeight = header.largeCharHeight,
                    Glyphs = largeGlyphs
                },
                SmallFont = new FontGlyphsData
                {
                    FallbackCharacter = smallGlyphs.Keys.ElementAtOrDefault(header.smallEscapeCharacterIndex),
                    MaxHeight = header.smallCharHeight,
                    Glyphs = smallGlyphs
                }
            };
        }

        private PlatformType GetPlatform(Fnt01Header header)
        {
            switch (header.magic[3])
            {
                case 'C':
                    return PlatformType.Ctr;

                case 'P':
                    return PlatformType.Psp;

                case 'V':
                    return PlatformType.PsVita;

                case 'A':
                    return PlatformType.Android;

                case 'N':
                    return PlatformType.Switch;

                default:
                    throw new InvalidOperationException($"Unknown platform identifier '{header.magic[3]}' in font.");
            }
        }

        private int GetVersion(Fnt01Header header)
        {
            return int.Parse(header.magic[4..6]);
        }
    }
}
