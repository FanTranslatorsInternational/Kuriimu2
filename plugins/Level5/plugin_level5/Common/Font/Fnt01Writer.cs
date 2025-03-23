using System.Text;
using Komponent.IO;
using plugin_level5.Common.Compression;
using plugin_level5.Common.Font.Models;

namespace plugin_level5.Common.Font
{
    public class Fnt01Writer : IFontWriter
    {
        private readonly Compressor _compressor = new();

        public void Write(FontData font, Stream output)
        {
            using var bw = new BinaryWriterX(output, true);

            FontGlyphsData largeFont = font.LargeFont;
            FontGlyphsData smallFont = font.SmallFont;

            IList<Fnt01CharSize> charactersSizes = new List<Fnt01CharSize>(largeFont.Glyphs.Count + smallFont.Glyphs.Count);
            Fnt01CharInfo[] largeCharacterInfos = CreateCharInfos(largeFont.Glyphs, charactersSizes);
            Fnt01CharInfo[] smallCharacterInfos = CreateCharInfos(smallFont.Glyphs, charactersSizes);

            long charSizeOffset = bw.BaseStream.Position = 0x28;
            WriteCharSizes(bw, charactersSizes);

            long largeCharOffset = bw.BaseStream.Position = (bw.BaseStream.Position + 3) & ~3;
            WriteCharInfos(bw, largeCharacterInfos);

            long smallCharOffset = bw.BaseStream.Position = (bw.BaseStream.Position + 3) & ~3;
            WriteCharInfos(bw, smallCharacterInfos);

            Fnt01Header header = CreateHeader(font, largeCharacterInfos, smallCharacterInfos, charactersSizes, charSizeOffset, largeCharOffset, smallCharOffset);

            bw.BaseStream.Position = 0;
            WriteHeader(bw, header);
        }

        private Fnt01CharInfo[] CreateCharInfos(IDictionary<char, FontGlyphData> glyphs, IList<Fnt01CharSize> charactersSizes)
        {
            var result = new Fnt01CharInfo[glyphs.Count];

            var index = 0;
            foreach (FontGlyphData glyph in glyphs.Values.OrderBy(x => x.CodePoint))
                result[index++] = CreateCharInfo(glyph, charactersSizes);

            return result;
        }

        private Fnt01CharInfo CreateCharInfo(FontGlyphData glyph, IList<Fnt01CharSize> charactersSizes)
        {
            uint imageInfo = (uint)(glyph.Location.Y & 0x3FFF) << 18;
            imageInfo |= (uint)(glyph.Location.X & 0x3FFF) << 4;
            imageInfo |= (uint)glyph.Location.Index & 0xF;

            Fnt01CharSize charSize = CreateCharSize(glyph.Description);
            int sizeIndex = charactersSizes.IndexOf(charSize);

            if (sizeIndex < 0)
            {
                sizeIndex = charactersSizes.Count;
                charactersSizes.Add(charSize);
            }

            var charSizeInfo = (ushort)((glyph.Width & 0x3F) << 10);
            charSizeInfo |= (ushort)(sizeIndex & 0x3FF);

            return new Fnt01CharInfo
            {
                charCode = glyph.CodePoint,
                charSizeInfo = charSizeInfo,
                imageInfo = imageInfo
            };
        }

        private Fnt01CharSize CreateCharSize(FontGlyphDescriptionData glyphDescription)
        {
            return new Fnt01CharSize
            {
                offsetX = glyphDescription.X,
                offsetY = glyphDescription.Y,
                glyphWidth = glyphDescription.Width,
                glyphHeight = glyphDescription.Height
            };
        }

        private Fnt01Header CreateHeader(FontData font, Fnt01CharInfo[] largeChars, Fnt01CharInfo[] smallChars, IList<Fnt01CharSize> charSizes, long charSizeOffset, long largeCharInfoOffset, long smallCharInfoOffset)
        {
            int escapeLargeCharIndex = Array.FindIndex(largeChars, info => info.charCode == font.LargeFont.FallbackCharacter);
            int escapeSmallCharIndex = Array.FindIndex(smallChars, info => info.charCode == font.SmallFont.FallbackCharacter);

            if (escapeLargeCharIndex < 0)
                escapeLargeCharIndex = 0;
            if (escapeSmallCharIndex < 0)
                escapeSmallCharIndex = 0;

            return new Fnt01Header
            {
                magic = GetMagic(font),
                version = 1,
                largeCharHeight = (short)font.LargeFont.MaxHeight,
                smallCharHeight = (short)font.SmallFont.MaxHeight,
                largeEscapeCharacterIndex = (ushort)escapeLargeCharIndex,
                smallEscapeCharacterIndex = (ushort)escapeSmallCharIndex,

                charSizeOffset = (short)(charSizeOffset >> 2),
                charSizeCount = (short)charSizes.Count,
                largeCharOffset = (short)(largeCharInfoOffset >> 2),
                largeCharCount = (short)largeChars.Length,
                smallCharOffset = (short)(smallCharInfoOffset >> 2),
                smallCharCount = (short)smallChars.Length
            };
        }

        private void WriteCharSizes(BinaryWriterX bw, IList<Fnt01CharSize> charactersSizes)
        {
            if (charactersSizes.Count <= 0)
            {
                bw.Write(1);
                bw.Write(0);

                return;
            }

            var ms = new MemoryStream();
            using var writer = new BinaryWriterX(ms, false);

            foreach (Fnt01CharSize characterSize in charactersSizes)
                WriteCharSize(writer, characterSize);

            ms.Position = 0;

            using Stream compressedCharSizes = _compressor.Compress(ms, Level5CompressionMethod.Huffman8Bit);
            compressedCharSizes.CopyTo(bw.BaseStream);
        }

        private void WriteCharSize(BinaryWriterX bw, Fnt01CharSize charSize)
        {
            bw.Write(charSize.offsetX);
            bw.Write(charSize.offsetY);
            bw.Write(charSize.glyphWidth);
            bw.Write(charSize.glyphHeight);
        }

        private void WriteCharInfos(BinaryWriterX bw, Fnt01CharInfo[] charInfos)
        {
            if (charInfos.Length <= 0)
            {
                bw.Write(1);
                bw.Write(0);

                return;
            }

            var ms = new MemoryStream();
            using var writer = new BinaryWriterX(ms, false);

            foreach (Fnt01CharInfo characterInfo in charInfos)
                WriteCharInfo(writer, characterInfo);

            ms.Position = 0;

            using Stream compressedCharSizes = _compressor.Compress(ms, Level5CompressionMethod.Huffman8Bit);
            compressedCharSizes.CopyTo(bw.BaseStream);
        }

        private void WriteCharInfo(BinaryWriterX bw, Fnt01CharInfo charInfo)
        {
            bw.Write(charInfo.charCode);
            bw.Write(charInfo.charSizeInfo);
            bw.Write(charInfo.imageInfo);
        }

        private void WriteHeader(BinaryWriterX bw, Fnt01Header header)
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
            bw.Write(header.largeCharOffset);
            bw.Write(header.largeCharCount);
            bw.Write(header.smallCharOffset);
            bw.Write(header.smallCharCount);
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
