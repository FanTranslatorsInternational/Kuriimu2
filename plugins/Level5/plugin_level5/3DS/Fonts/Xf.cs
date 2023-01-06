using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Komponent.Font;
using Komponent.IO;
using Kontract.Models.Plugins.State.Font;
using plugin_level5.Compression;
using plugin_level5.Extensions;

namespace plugin_level5._3DS.Fonts
{
    public class Xf
    {
        private Level5CompressionMethod _t0Comp;
        private Level5CompressionMethod _t1Comp;
        private Level5CompressionMethod _t2Comp;

        private readonly ColorMatrix[] _colorMatrices =
        {
            new ColorMatrix(new[]
            {
                new[] { 0f, 0, 0, 1f, 0 },
                new[] { 0, 0f, 0, 0, 0 },
                new[] { 0, 0, 0f, 0, 0 },
                new[] { 0, 0, 0, 0f, 0 },
                new[] { 1f, 1f, 1f, 0, 1f }
            }),
            new ColorMatrix(new[]
            {
                new[] { 0f, 0, 0, 0, 0 },
                new[] { 0, 0f, 0, 1f, 0 },
                new[] { 0, 0, 0f, 0, 0 },
                new[] { 0, 0, 0, 0f, 0 },
                new[] { 1f, 1f, 1f, 0, 1f }
            }),
            new ColorMatrix(new[]
            {
                new[] { 0f, 0, 0, 0, 0 },
                new[] { 0, 0f, 0, 0, 0 },
                new[] { 0, 0, 0f, 1f, 0 },
                new[] { 0, 0, 0, 0f, 0 },
                new[] { 1f, 1f, 1f, 0, 1f }
            })
        };

        private readonly ColorMatrix[] _inverseColorMatrices =
        {
            new ColorMatrix(new[]
            {
                new[] { 0f, 0, 0, 0, 0 },
                new[] { 0, 0f, 0, 0, 0 },
                new[] { 0, 0, 0f, 0, 0 },
                new[] { 1f, 0, 0, 0f, 0 },
                new[] { 0, 0, 0, 1f, 1f }
            }),
            new ColorMatrix(new[]
            {
                new[] { 0f, 0, 0, 0, 0 },
                new[] { 0, 0f, 0, 0, 0 },
                new[] { 0, 0, 0f, 0, 0 },
                new[] { 0, 1f, 0, 0f, 0 },
                new[] { 0, 0, 0, 1f, 1f }
            }),
            new ColorMatrix(new[]
            {
                new[] { 0f, 0, 0, 0, 0 },
                new[] { 0, 0f, 0, 0, 0 },
                new[] { 0, 0, 0f, 0, 0 },
                new[] { 0, 0, 1f, 0f, 0 },
                new[] { 0, 0, 0, 1f, 1f }
            })
        };

        public XfHeader Header { get; private set; }

        public List<CharacterInfo> Load(Stream fntFile, Image fontImage)
        {
            using var br = new BinaryReaderX(fntFile);

            //var f = File.Create(@"D:\Users\Kirito\Desktop\reverse_engineering\time_travelers\font\test.bin");
            //br.BaseStream.CopyTo(f);
            //f.Close();
            //br.BaseStream.Position = 0;

            // Read header
            Header = br.ReadType<XfHeader>();

            // Read charSizeInfo
            br.BaseStream.Position = Header.charSizeOffset << 2;
            _t0Comp = Level5Compressor.PeekCompressionMethod(br.BaseStream);
            var charSizeStream = Decompress(br.BaseStream);
            var charSizeInfos = new BinaryReaderX(charSizeStream).ReadMultiple<XfCharSizeInfo>(Header.charSizeCount).ToArray();

            // Read large chars
            br.BaseStream.Position = Header.largeCharOffset << 2;
            _t1Comp = Level5Compressor.PeekCompressionMethod(br.BaseStream);
            var largeCharStream = Decompress(br.BaseStream);
            var largeChars = new BinaryReaderX(largeCharStream).ReadMultiple<XfCharMap>(Header.largeCharCount).ToArray();

            // Read small chars
            br.BaseStream.Position = Header.smallCharOffset << 2;
            _t2Comp = Level5Compressor.PeekCompressionMethod(br.BaseStream);
            var smallCharStream = Decompress(br.BaseStream);
            var smallChars = new BinaryReaderX(smallCharStream).ReadMultiple<XfCharMap>(Header.smallCharCount).ToArray();

            // Load characters (ignore small chars)
            var result = new List<CharacterInfo>(largeChars.Length);
            foreach (var largeChar in largeChars)
            {
                var charSizeInfo = charSizeInfos[largeChar.charInformation.charSizeInfoIndex];
                var glyph = GetGlyphBitmap(fontImage, largeChar, charSizeInfo);

                var characterInfo = new CharacterInfo(largeChar.codePoint, new Size(largeChar.charInformation.charWidth, 0), glyph);
                result.Add(characterInfo);
            }

            return result;
        }

        public (Stream fontStream, Image fontImage) Save(List<CharacterInfo> characterInfos, Size imageSize)
        {
            // Generating font textures
            var adjustedGlyphs = FontMeasurement.MeasureWhiteSpace(characterInfos.Select(x => (Bitmap)x.Glyph)).ToList();

            // Adjust image size for at least the biggest letter
            var height = Math.Max(adjustedGlyphs.Max(x => x.WhiteSpaceAdjustment.GlyphSize.Height), imageSize.Height);
            var width = Math.Max(adjustedGlyphs.Max(x => x.WhiteSpaceAdjustment.GlyphSize.Width), imageSize.Width);
            imageSize = new Size(width, height);

            var generator = new FontTextureGenerator(imageSize, 0);
            var textureInfos = generator.GenerateFontTextures(adjustedGlyphs, 3).ToList();

            // Join important lists
            var joinedCharacters = characterInfos.OrderBy(x => x.CodePoint).Join(adjustedGlyphs, c => c.Glyph, ag => ag.Glyph,
                (c, ag) => new { character = c, adjustedGlyph = ag })
                .Select(cag => new
                {
                    cag.character,
                    cag.adjustedGlyph,
                    textureIndex = textureInfos.FindIndex(x => x.Glyphs.Any(y => y.Item1 == cag.adjustedGlyph.Glyph)),
                    texturePosition = textureInfos.SelectMany(x => x.Glyphs).FirstOrDefault(x => x.Item1 == cag.adjustedGlyph.Glyph).Item2
                });

            // Create character information
            var charMaps = new List<(AdjustedGlyph, XfCharMap)>(adjustedGlyphs.Count);
            var charSizeInfos = new List<XfCharSizeInfo>();
            foreach (var joinedCharacter in joinedCharacters)
            {
                if (joinedCharacter.textureIndex == -1)
                    continue;

                var charSizeInfo = new XfCharSizeInfo
                {
                    offsetX = (sbyte)joinedCharacter.adjustedGlyph.WhiteSpaceAdjustment.GlyphPosition.X,
                    offsetY = (sbyte)joinedCharacter.adjustedGlyph.WhiteSpaceAdjustment.GlyphPosition.Y,
                    glyphWidth = (byte)joinedCharacter.adjustedGlyph.WhiteSpaceAdjustment.GlyphSize.Width,
                    glyphHeight = (byte)joinedCharacter.adjustedGlyph.WhiteSpaceAdjustment.GlyphSize.Height
                };
                if (!charSizeInfos.Contains(charSizeInfo))
                    charSizeInfos.Add(charSizeInfo);

                // Only used for Time Travelers
                var codePoint = ConvertChar(joinedCharacter.character.CodePoint);
                //var codePoint = joinedCharacter.character.CodePoint;

                var charInformation = new XfCharInformation
                {
                    charSizeInfoIndex = charSizeInfos.IndexOf(charSizeInfo),
                    charWidth = char.IsWhiteSpace((char)codePoint) ?
                        joinedCharacter.character.CharacterSize.Width :
                        joinedCharacter.character.CharacterSize.Width - charSizeInfo.offsetX
                };

                charMaps.Add((joinedCharacter.adjustedGlyph, new XfCharMap
                {
                    codePoint = (ushort)codePoint,
                    charInformation = charInformation,
                    imageInformation = new XfImageInformation
                    {
                        colorChannel = joinedCharacter.textureIndex,
                        imageOffsetX = joinedCharacter.texturePosition.X,
                        imageOffsetY = joinedCharacter.texturePosition.Y
                    }
                }));

                if (codePoint != joinedCharacter.character.CodePoint)
                {
                    charInformation = new XfCharInformation
                    {
                        charSizeInfoIndex = charSizeInfos.IndexOf(charSizeInfo),
                        charWidth = char.IsWhiteSpace((char)joinedCharacter.character.CodePoint)
                            ? joinedCharacter.character.CharacterSize.Width
                            : joinedCharacter.character.CharacterSize.Width - charSizeInfo.offsetX
                    };

                    charMaps.Add((joinedCharacter.adjustedGlyph, new XfCharMap
                    {
                        codePoint = (ushort)joinedCharacter.character.CodePoint,
                        charInformation = charInformation,
                        imageInformation = new XfImageInformation
                        {
                            colorChannel = joinedCharacter.textureIndex,
                            imageOffsetX = joinedCharacter.texturePosition.X,
                            imageOffsetY = joinedCharacter.texturePosition.Y
                        }
                    }));
                }
            }

            // Set escape characters
            var escapeIndex = charMaps.FindIndex(x => x.Item2.codePoint == '?');
            Header.largeEscapeCharacter = escapeIndex < 0 ? (short)0 : (short)escapeIndex;
            Header.smallEscapeCharacter = 0;

            // Minimize top value and line height
            Header.largeCharHeight = (short)charSizeInfos.Max(x => x.glyphHeight + x.offsetY);
            Header.smallCharHeight = 0;

            // Draw textures
            var img = new Bitmap(imageSize.Width, imageSize.Height);
            var chn = new Bitmap(imageSize.Width, imageSize.Height);
            var gfx = Graphics.FromImage(chn);
            for (var i = 0; i < textureInfos.Count; i++)
            {
                var destPoints = new[]
                {
                    new PointF(0,0),
                    new PointF(textureInfos[i].FontTexture.Width,0),
                    new PointF(0,textureInfos[i].FontTexture.Height)
                };
                var rect = new RectangleF(0, 0, textureInfos[i].FontTexture.Width, textureInfos[i].FontTexture.Height);
                var attr = new ImageAttributes();
                attr.SetColorMatrix(_inverseColorMatrices[i]);
                gfx.DrawImage(textureInfos[i].FontTexture, destPoints, rect, GraphicsUnit.Pixel, attr);
                img.PutChannel(chn);
            }

            // Save fnt.bin
            var savedFntBin = new MemoryStream();
            using (var bw = new BinaryWriterX(savedFntBin, true))
            {
                //Table0
                bw.BaseStream.Position = 0x28;
                Header.charSizeCount = (short)charSizeInfos.Count;
                WriteMultipleCompressed(bw, charSizeInfos, _t0Comp);
                bw.WriteAlignment(4);

                //Table1
                Header.largeCharOffset = (short)(bw.BaseStream.Position >> 2);
                Header.largeCharCount = (short)charMaps.Count;
                WriteMultipleCompressed(bw, charMaps.OrderBy(x => x.Item2.codePoint).Select(x => x.Item2).ToArray(), _t1Comp);
                bw.WriteAlignment(4);

                //Table2
                Header.smallCharOffset = (short)(bw.BaseStream.Position >> 2);
                Header.smallCharCount = 0;
                WriteMultipleCompressed(bw, Array.Empty<XfCharMap>(), _t2Comp);
                bw.WriteAlignment(4);

                //Header
                bw.BaseStream.Position = 0;
                bw.WriteType(Header);
            }

            return (savedFntBin, img);
        }

        private Bitmap GetGlyphBitmap(Image fontImage, XfCharMap charMap, XfCharSizeInfo charSizeInfo)
        {
            // Destination points
            var destPoints = new[]
            {
                new PointF(charSizeInfo.offsetX, charSizeInfo.offsetY),
                new PointF(charSizeInfo.glyphWidth+charSizeInfo.offsetX, charSizeInfo.offsetY),
                new PointF(charSizeInfo.offsetX, charSizeInfo.glyphHeight+charSizeInfo.offsetY)
            };

            // Source rectangle
            var srcRect = new RectangleF(
                charMap.imageInformation.imageOffsetX,
                charMap.imageInformation.imageOffsetY,
                charSizeInfo.glyphWidth,
                charSizeInfo.glyphHeight);

            // Color matrix
            var imageAttributes = new ImageAttributes();
            imageAttributes.SetColorMatrix(_colorMatrices[charMap.imageInformation.colorChannel]);

            // Draw the glyph from the master texture
            var glyph = new Bitmap(
                Math.Max(1, Math.Max(charMap.charInformation.charWidth, charSizeInfo.glyphWidth + charSizeInfo.offsetX)),
                Math.Max(1, charSizeInfo.glyphHeight + charSizeInfo.offsetY));
            var gfx = Graphics.FromImage(glyph);
            gfx.DrawImage(fontImage, destPoints, srcRect, GraphicsUnit.Pixel, imageAttributes);

            return glyph;
        }

        private Stream Decompress(Stream input)
        {
            var output = new MemoryStream();

            Level5Compressor.Decompress(input, output);
            output.Position = 0;

            return output;
        }

        private void WriteMultipleCompressed<T>(BinaryWriterX bw, IList<T> list, Level5CompressionMethod comp)
        {
            var ms = new MemoryStream();
            using (var bwOut = new BinaryWriterX(ms, true))
                bwOut.WriteMultiple(list);

            var compressedStream = new MemoryStream();
            ms.Position = 0;
            Level5Compressor.Compress(ms, compressedStream, comp);

            compressedStream.Position = 0;
            compressedStream.CopyTo(bw.BaseStream);
        }

        private uint ConvertChar(uint character)
        {
            // Specially handle space
            if (character == 0x20)
                return 0x3000;

            // Convert all other letters
            if (character >= 0x21 && character <= 0x7E)
                return character + 0xFEE0;

            return character;
        }
    }
}
