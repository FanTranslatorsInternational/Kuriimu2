using System.Buffers.Binary;
using System.Text;
using Komponent.IO;
using plugin_level5.Common.Compression;
using plugin_level5.Common.Font.Models;

namespace plugin_level5.Common.Font
{
    public class Fnt00Writer : IFontWriter
    {
        private readonly Encoding _sjis;
        private readonly Compressor _compressor = new();

        public Fnt00Writer()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _sjis = Encoding.GetEncoding("Shift-JIS");
        }

        public void Write(FontData font, Stream output)
        {
            using var bw = new BinaryWriterX(output, true);

            FontGlyphsData largeFont = font.LargeFont;
            FontGlyphsData smallFont = font.SmallFont;

            IList<Fnt00CharSize> charactersSizes = new List<Fnt00CharSize>(largeFont.Glyphs.Count + smallFont.Glyphs.Count);
            Fnt00CharInfo[] largeUnicodeCharacterInfos = CreateCharInfos(largeFont.Glyphs, charactersSizes, true);
            Fnt00CharInfo[] smallUnicodeCharacterInfos = CreateCharInfos(smallFont.Glyphs, charactersSizes, true);
            Fnt00CharInfo[] largeSjisCharacterInfos = CreateCharInfos(largeFont.Glyphs, charactersSizes, false);
            Fnt00CharInfo[] smallSjisCharacterInfos = CreateCharInfos(smallFont.Glyphs, charactersSizes, false);

            long charSizeOffset = bw.BaseStream.Position = 0x30;
            WriteCharSizes(bw, charactersSizes);

            long largeSjisCharOffset = bw.BaseStream.Position = (bw.BaseStream.Position + 3) & ~3;
            WriteCharInfos(bw, largeSjisCharacterInfos);

            long smallSjisCharOffset = bw.BaseStream.Position = (bw.BaseStream.Position + 3) & ~3;
            WriteCharInfos(bw, smallSjisCharacterInfos);

            long largeUnicodeCharOffset = bw.BaseStream.Position = (bw.BaseStream.Position + 3) & ~3;
            WriteCharInfos(bw, largeUnicodeCharacterInfos);

            long smallUnicodeCharOffset = bw.BaseStream.Position = (bw.BaseStream.Position + 3) & ~3;
            WriteCharInfos(bw, smallUnicodeCharacterInfos);

            Fnt00Header header = CreateHeader(font, largeSjisCharacterInfos, smallSjisCharacterInfos,
                largeUnicodeCharacterInfos, smallUnicodeCharacterInfos, charactersSizes,
                charSizeOffset, largeSjisCharOffset, smallSjisCharOffset, largeUnicodeCharOffset, smallUnicodeCharOffset);

            bw.BaseStream.Position = 0;
            WriteHeader(bw, header);
        }

        private Fnt00CharInfo[] CreateCharInfos(IDictionary<char, FontGlyphData> glyphs, IList<Fnt00CharSize> charactersSizes, bool isUnicode)
        {
            var result = new Fnt00CharInfo[glyphs.Count];

            var index = 0;
            foreach (FontGlyphData glyph in glyphs.Values.OrderBy(x => x.CodePoint))
            {
                Fnt00CharInfo? charInfo = CreateCharInfo(glyph, charactersSizes, isUnicode);
                if (!charInfo.HasValue)
                    continue;

                result[index++] = charInfo.Value;
            }

            return result;
        }

        private Fnt00CharInfo? CreateCharInfo(FontGlyphData glyph, IList<Fnt00CharSize> charactersSizes, bool isUnicode)
        {
            Fnt00CharSize charSize = CreateCharSize(glyph);
            int sizeIndex = charactersSizes.IndexOf(charSize);

            if (sizeIndex < 0)
            {
                sizeIndex = charactersSizes.Count;
                charactersSizes.Add(charSize);
            }

            ushort codePoint = glyph.CodePoint;
            if (!isUnicode)
            {
                if (!TryGetSjisCodePoint(codePoint, out ushort sjisCodePoint))
                    return null;

                codePoint = sjisCodePoint;
            }

            return new Fnt00CharInfo
            {
                charCode = codePoint,
                charSizeIndex = (ushort)sizeIndex
            };
        }

        private bool TryGetSjisCodePoint(ushort unicodeCodePoint, out ushort sjisCodePoint)
        {
            sjisCodePoint = 0;

            byte[] sjisBuffer = _sjis.GetBytes($"{unicodeCodePoint}");
            switch (sjisBuffer.Length)
            {
                case 1:
                    sjisCodePoint = sjisBuffer[0];
                    return true;

                case 2:
                    sjisCodePoint = BinaryPrimitives.ReadUInt16BigEndian(sjisBuffer);
                    return true;

                default:
                    return false;
            }
        }

        private Fnt00CharSize CreateCharSize(FontGlyphData glyph)
        {
            uint imageInfo = (uint)(glyph.Location.Y & 0x3FF) << 22;
            imageInfo |= (uint)(glyph.Location.X & 0x3FF) << 12;
            imageInfo |= (uint)(glyph.Location.Index & 0xF) << 8;
            imageInfo |= (uint)glyph.Width & 0xFF;

            return new Fnt00CharSize
            {
                imageInfo = imageInfo,
                offsetX = glyph.Description.X,
                offsetY = glyph.Description.Y,
                glyphWidth = glyph.Description.Width,
                glyphHeight = glyph.Description.Height
            };
        }

        private void WriteCharSizes(BinaryWriterX bw, IList<Fnt00CharSize> charactersSizes)
        {
            if (charactersSizes.Count <= 0)
            {
                bw.Write(1);
                bw.Write(0);

                return;
            }

            var ms = new MemoryStream();
            using var writer = new BinaryWriterX(ms, false);

            foreach (Fnt00CharSize characterSize in charactersSizes)
                WriteCharSize(writer, characterSize);

            ms.Position = 0;

            using Stream compressedCharSizes = _compressor.Compress(ms, Level5CompressionMethod.Huffman8Bit);
            compressedCharSizes.CopyTo(bw.BaseStream);
        }

        private void WriteCharSize(BinaryWriterX bw, Fnt00CharSize charSize)
        {
            bw.Write(charSize.imageInfo);
            bw.Write(charSize.offsetX);
            bw.Write(charSize.offsetY);
            bw.Write(charSize.glyphWidth);
            bw.Write(charSize.glyphHeight);
        }

        private void WriteCharInfos(BinaryWriterX bw, Fnt00CharInfo[] charInfos)
        {
            if (charInfos.Length <= 0)
            {
                bw.Write(1);
                bw.Write(0);

                return;
            }

            var ms = new MemoryStream();
            using var writer = new BinaryWriterX(ms, false);

            foreach (Fnt00CharInfo characterInfo in charInfos)
                WriteCharInfo(writer, characterInfo);

            ms.Position = 0;

            using Stream compressedCharSizes = _compressor.Compress(ms, Level5CompressionMethod.Huffman8Bit);
            compressedCharSizes.CopyTo(bw.BaseStream);
        }

        private void WriteCharInfo(BinaryWriterX bw, Fnt00CharInfo charInfo)
        {
            bw.Write(charInfo.charCode);
            bw.Write(charInfo.charSizeIndex);
        }

        private Fnt00Header CreateHeader(FontData font, Fnt00CharInfo[] largeSjisChars, Fnt00CharInfo[] smallSjisChars,
            Fnt00CharInfo[] largeUnicodeChars, Fnt00CharInfo[] smallUnicodeChars, IList<Fnt00CharSize> charSizes,
            long charSizeOffset, long largeSjisCharInfoOffset, long smallSjisCharInfoOffset, long largeUnicodeCharInfoOffset, long smallUnicodeCharInfoOffset)
        {
            int escapeLargeCharIndex = Array.FindIndex(largeSjisChars, info => info.charCode == font.LargeFont.FallbackCharacter);
            int escapeSmallCharIndex = Array.FindIndex(smallSjisChars, info => info.charCode == font.SmallFont.FallbackCharacter);

            if (escapeLargeCharIndex < 0)
            {
                escapeLargeCharIndex = Array.FindIndex(largeUnicodeChars, info => info.charCode == font.LargeFont.FallbackCharacter);
                if (escapeLargeCharIndex < 0)
                    escapeLargeCharIndex = 0;
            }

            if (escapeSmallCharIndex < 0)
            {
                escapeSmallCharIndex = Array.FindIndex(smallUnicodeChars, info => info.charCode == font.LargeFont.FallbackCharacter);
                if (escapeSmallCharIndex < 0)
                    escapeSmallCharIndex = 0;
            }

            return new Fnt00Header
            {
                magic = GetMagic(font),
                version = 1,
                largeCharHeight = (short)font.LargeFont.MaxHeight,
                smallCharHeight = (short)font.SmallFont.MaxHeight,
                largeEscapeCharacterIndex = (ushort)escapeLargeCharIndex,
                smallEscapeCharacterIndex = (ushort)escapeSmallCharIndex,

                charSizeOffset = (short)(charSizeOffset >> 2),
                charSizeCount = (short)charSizes.Count,
                largeCharSjisOffset = (short)(largeSjisCharInfoOffset >> 2),
                largeCharSjisCount = (short)largeSjisChars.Length,
                smallCharSjisOffset = (short)(smallSjisCharInfoOffset >> 2),
                smallCharSjisCount = (short)smallSjisChars.Length,
                largeCharUnicodeOffset = (short)(largeUnicodeCharInfoOffset >> 2),
                largeCharUnicodeCount = (short)largeUnicodeChars.Length,
                smallCharUnicodeOffset = (short)(smallUnicodeCharInfoOffset >> 2),
                smallCharUnicodeCount = (short)smallUnicodeChars.Length
            };
        }

        private void WriteHeader(BinaryWriterX bw, Fnt00Header header)
        {
            bw.WriteString(header.magic, Encoding.ASCII, false, false);

            bw.Write(header.version);
            bw.Write(header.largeCharHeight);
            bw.Write(header.smallCharHeight);
            bw.Write(header.largeEscapeCharacterIndex);
            bw.Write(header.smallEscapeCharacterIndex);
            bw.Write(header.zero0);

            bw.Write(header.charSizeOffset);
            bw.Write(header.charSizeCount);
            bw.Write(header.largeCharSjisOffset);
            bw.Write(header.largeCharSjisCount);
            bw.Write(header.smallCharSjisOffset);
            bw.Write(header.smallCharSjisCount);
            bw.Write(header.largeCharUnicodeOffset);
            bw.Write(header.largeCharUnicodeCount);
            bw.Write(header.smallCharUnicodeOffset);
            bw.Write(header.smallCharUnicodeCount);
        }

        private string GetMagic(FontData font)
        {
            char identifier = GetPlatformIdentifier(font.Version.Platform);
            string version = GetPlatformVersion(font.Version.Version);

            return $"FNT{identifier}{version}\0\0";
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
                    throw new InvalidOperationException($"Unknown platform {platform} for font.");
            }
        }

        private string GetPlatformVersion(int version)
        {
            return $"{version:00}";
        }
    }
}
