using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Komponent.Font;
using Komponent.IO;
using Kontract.Interfaces.Font;
using Level5.Fonts.Compression;
using Level5.Fonts.Models;

namespace Level5.Fonts
{
    public class XF
    {
        private XPCK.XPCK _xpck;
        private IMGC.IMGC _xi;

        private CompressionMethod _t0Comp;
        private CompressionMethod _t1Comp;
        private CompressionMethod _t2Comp;

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

        public XfHeader Header;

        public List<FontCharacter2> Characters { get; set; }

        public XF(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Load archive
                _xpck = new XPCK.XPCK(input);

                // Get image
                _xi = new IMGC.IMGC(_xpck.Files[0].FileData);

                // Decompress fnt.bin
                XfCharSizeInfo[] tempCharSizeInfo;
                XfCharMap[] largeChars;
                XfCharMap[] smallChars;
                using (var fntR = new BinaryReaderX(_xpck.Files[1].FileData, true))
                {
                    Header = fntR.ReadType<XfHeader>();

                    fntR.BaseStream.Position = Header.table0Offset << 2;
                    _t0Comp = (CompressionMethod)(fntR.ReadInt32() & 0x7);
                    fntR.BaseStream.Position -= 4;
                    var compBr = new BinaryReaderX(new MemoryStream(Compressor.Decompress(fntR.BaseStream)));
                    tempCharSizeInfo = compBr.ReadMultiple<XfCharSizeInfo>(Header.table0EntryCount).ToArray();

                    fntR.BaseStream.Position = Header.table1Offset << 2;
                    _t1Comp = (CompressionMethod)(fntR.ReadInt32() & 0x7);
                    fntR.BaseStream.Position -= 4;
                    compBr = new BinaryReaderX(new MemoryStream(Compressor.Decompress(fntR.BaseStream)));
                    largeChars = compBr.ReadMultiple<XfCharMap>(Header.table1EntryCount).ToArray();

                    fntR.BaseStream.Position = Header.table2Offset << 2;
                    _t2Comp = (CompressionMethod)(fntR.ReadInt32() & 0x7);
                    fntR.BaseStream.Position -= 4;
                    compBr = new BinaryReaderX(new MemoryStream(Compressor.Decompress(fntR.BaseStream)));
                    smallChars = compBr.ReadMultiple<XfCharMap>(Header.table2EntryCount).ToArray();
                }

                // Set Characters
                Characters = new List<FontCharacter2>(largeChars.Length);
                foreach (var charMap in largeChars)
                {
                    var glyph = GetGlyphBitmap(charMap, tempCharSizeInfo[charMap.charInformation.charSizeInfoIndex]);

                    var characterInfo = new CharacterInfo(charMap.charInformation.charWidth);
                    var character = new FontCharacter2(charMap.codePoint)
                    {
                        Glyph = glyph,
                        CharacterInfo = characterInfo
                    };
                    Characters.Add(character);
                }
                /*foreach (var charMap in smallChars)
                    Characters.Add(new XfCharacter
                    {
                        Character = charMap.code_point,
                        TextureID = (int)charMap.ColorChannel,
                        GlyphX = (int)charMap.ImageOffsetX,
                        GlyphY = (int)charMap.ImageOffsetY,
                        GlyphWidth = tempCharSizeInfo[(int)charMap.CharSizeInfoIndex].glyph_width,
                        GlyphHeight = tempCharSizeInfo[(int)charMap.CharSizeInfoIndex].glyph_height,
                        CharacterWidth = (int)charMap.CharWidth,
                        OffsetX = tempCharSizeInfo[(int)charMap.CharSizeInfoIndex].offset_x,
                        OffsetY = tempCharSizeInfo[(int)charMap.CharSizeInfoIndex].offset_y,
                    });*/
            }
        }

        public void Save(Stream output)
        {
            // Generating font textures
            var generator = new FontTextureGenerator(_xi.Image.Size);

            var adjustedGlyphs = FontMeasurement.MeasureWhiteSpace(Characters.Select(x => x.Glyph)).ToList();
            var textureInfos = generator.GenerateFontTextures(adjustedGlyphs, 3).ToList();

            // Join important lists
            var joinedCharacters = Characters.OrderBy(x => x.Character).Join(adjustedGlyphs, c => c.Glyph, ag => ag.Glyph,
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

                var charInformation = new XfCharInformation
                {
                    charSizeInfoIndex = charSizeInfos.IndexOf(charSizeInfo),
                    charWidth = char.IsWhiteSpace((char)joinedCharacter.character.Character) ?
                        joinedCharacter.character.CharacterInfo.CharWidth :
                        joinedCharacter.character.CharacterInfo.CharWidth - charSizeInfo.offsetX
                };

                charMaps.Add((joinedCharacter.adjustedGlyph, new XfCharMap
                {
                    codePoint = (ushort)joinedCharacter.character.Character,
                    charInformation = charInformation,
                    imageInformation = new XfImageInformation
                    {
                        colorChannel = joinedCharacter.textureIndex,
                        imageOffsetX = joinedCharacter.texturePosition.X,
                        imageOffsetY = joinedCharacter.texturePosition.Y
                    }
                }));
            }

            // Set escape characters
            Header.largeEscapeCharacter = (short)charMaps.FindIndex(x => x.Item2.codePoint == '?');
            Header.smallEscapeCharacter = 0;

            // Minimize top value and line height
            Header.largeCharHeight = (short)charSizeInfos.Max(x => x.glyphHeight + x.offsetY);
            Header.smallCharHeight = 0;

            // Draw textures
            var img = new Bitmap(_xi.Image.Width, _xi.Image.Height);
            var gfx = Graphics.FromImage(img);
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
            }

            // Save xi image
            _xi.Image = img;
            var savedXi = new MemoryStream();
            _xi.Save(savedXi);
            _xpck.Files[0].FileData = savedXi;

            // Save fnt.bin
            var savedFntBin = new MemoryStream();
            using (var bw = new BinaryWriterX(savedFntBin, true))
            {
                //Table0
                Header.table0EntryCount = (short)charSizeInfos.Count;
                bw.BaseStream.Position = 0x28;
                bw.WriteMultipleCompressed(charSizeInfos, _t0Comp);
                bw.WriteAlignment(4);

                //Table1
                Header.table1Offset = (short)(bw.BaseStream.Position >> 2);
                Header.table1EntryCount = (short)charMaps.Count;
                bw.WriteMultipleCompressed(charMaps.Select(d => d.Item2), _t1Comp);
                bw.WriteAlignment(4);

                //Table2
                Header.table2Offset = (short)(bw.BaseStream.Position >> 2);
                Header.table2EntryCount = 0;
                bw.WriteMultipleCompressed(Array.Empty<XfCharMap>(), _t2Comp);
                bw.WriteAlignment(4);

                //Header
                bw.BaseStream.Position = 0;
                bw.WriteType(Header);
            }
            _xpck.Files[1].FileData = savedFntBin;

            _xpck.Save(output);
        }

        private Bitmap GetGlyphBitmap(XfCharMap charMap, XfCharSizeInfo charSizeInfo)
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
            gfx.DrawImage(_xi.Image, destPoints, srcRect, GraphicsUnit.Pixel, imageAttributes);

            return glyph;
        }
    }

    public static class XFExtensions
    {
        public static void WriteMultipleCompressed<T>(this BinaryWriterX bw, IEnumerable<T> list, CompressionMethod comp)
        {
            var ms = new MemoryStream();
            using (var bwIntern = new BinaryWriterX(ms, true))
                foreach (var t in list)
                    bwIntern.WriteType(t);
            bw.Write(Compressor.Compress(ms, comp));
        }
    }
}