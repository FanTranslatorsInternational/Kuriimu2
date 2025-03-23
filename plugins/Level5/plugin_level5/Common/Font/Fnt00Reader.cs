using Komponent.IO;
using Komponent.Streams;
using plugin_level5.Common.Compression;
using plugin_level5.Common.Font.Models;

namespace plugin_level5.Common.Font
{
    public class Fnt00Reader : IFontReader
    {
        private readonly Decompressor _decompressor = new();

        public FontData Read(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            Fnt00Header header = ReadHeader(br);

            Fnt00CharSize[] charactersSizes = ReadCharSizes(br, header);
            Fnt00CharInfo[] largeCharacterSjisInfos = ReadCharInfos(br, header.largeCharSjisOffset << 2, header.largeCharSjisCount);
            Fnt00CharInfo[] smallCharacterSjisInfos = ReadCharInfos(br, header.smallCharSjisOffset << 2, header.smallCharSjisCount);
            Fnt00CharInfo[] largeCharacterUnicodeInfos = ReadCharInfos(br, header.largeCharUnicodeOffset << 2, header.largeCharUnicodeCount);
            Fnt00CharInfo[] smallCharacterUnicodeInfos = ReadCharInfos(br, header.smallCharUnicodeOffset << 2, header.smallCharUnicodeCount);

            IDictionary<char, FontGlyphData> largeGlyphs = CreateGlyphs(charactersSizes, largeCharacterUnicodeInfos);
            IDictionary<char, FontGlyphData> smallGlyphs = CreateGlyphs(charactersSizes, smallCharacterUnicodeInfos);

            return CreateFontData(header, largeGlyphs, smallGlyphs);
        }

        private Fnt00Header ReadHeader(BinaryReaderX br)
        {
            return new Fnt00Header
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

                largeCharSjisOffset = br.ReadInt16(),
                largeCharSjisCount = br.ReadInt16(),
                smallCharSjisOffset = br.ReadInt16(),
                smallCharSjisCount = br.ReadInt16(),

                largeCharUnicodeOffset = br.ReadInt16(),
                largeCharUnicodeCount = br.ReadInt16(),
                smallCharUnicodeOffset = br.ReadInt16(),
                smallCharUnicodeCount = br.ReadInt16()
            };
        }

        private Fnt00CharSize[] ReadCharSizes(BinaryReaderX br, Fnt00Header header)
        {
            using Stream compressedCharSizes = new SubStream(br.BaseStream, header.charSizeOffset << 2);
            using Stream decompressedCharSizes = _decompressor.Decompress(compressedCharSizes, 0);

            using var sizeReader = new BinaryReaderX(decompressedCharSizes);

            var result = new Fnt00CharSize[header.charSizeCount];
            for (var i = 0; i < header.charSizeCount; i++)
                result[i] = ReadCharSize(sizeReader);

            return result;
        }

        private Fnt00CharSize ReadCharSize(BinaryReaderX br)
        {
            return new Fnt00CharSize
            {
                imageInfo = br.ReadUInt32(),
                offsetX = br.ReadSByte(),
                offsetY = br.ReadSByte(),
                glyphWidth = br.ReadByte(),
                glyphHeight = br.ReadByte()
            };
        }

        private Fnt00CharInfo[] ReadCharInfos(BinaryReaderX br, int charInfoOffset, int charInfoCount)
        {
            using Stream compressedCharInfos = new SubStream(br.BaseStream, charInfoOffset);
            using Stream decompressedCharInfos = _decompressor.Decompress(compressedCharInfos, 0);

            using var infoReader = new BinaryReaderX(decompressedCharInfos);

            var result = new Fnt00CharInfo[charInfoCount];
            for (var i = 0; i < charInfoCount; i++)
                result[i] = ReadCharInfo(infoReader);

            return result;
        }

        private Fnt00CharInfo ReadCharInfo(BinaryReaderX br)
        {
            return new Fnt00CharInfo
            {
                charCode = br.ReadUInt16(),
                charSizeIndex = br.ReadUInt16()
            };
        }

        private IDictionary<char, FontGlyphData> CreateGlyphs(Fnt00CharSize[] characterSizes, Fnt00CharInfo[] characterInfos)
        {
            var result = new Dictionary<char, FontGlyphData>();
            for (var i = 0; i < characterInfos.Length; i++)
                result[(char)characterInfos[i].charCode] = CreateGlyph(characterSizes[characterInfos[i].charSizeIndex], characterInfos[i]);

            return result;
        }

        private FontGlyphData CreateGlyph(Fnt00CharSize characterSize, Fnt00CharInfo characterInfo)
        {
            return new FontGlyphData
            {
                CodePoint = characterInfo.charCode,
                Width = (int)(characterSize.imageInfo & 0xFF),
                Location = new FontGlyphLocationData
                {
                    Y = (int)(characterSize.imageInfo >> 22),
                    X = (int)((characterSize.imageInfo >> 12) & 0x3FF),
                    Index = (int)((characterSize.imageInfo >> 8) & 0xF)
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

        private FontData CreateFontData(Fnt00Header header, IDictionary<char, FontGlyphData> largeGlyphs, IDictionary<char, FontGlyphData> smallGlyphs)
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

        private PlatformType GetPlatform(Fnt00Header header)
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

        private int GetVersion(Fnt00Header header)
        {
            return int.Parse(header.magic[4..6]);
        }
    }
}
