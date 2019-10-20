using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
            }),
        };

        public Dictionary<char, XfCharSizeInfo> lstCharSizeInfoLarge;
        public Dictionary<char, XfCharSizeInfo> lstCharSizeInfoSmall;
        public Dictionary<char, XfCharSizeInfo> dicGlyphLarge;
        public Dictionary<char, XfCharSizeInfo> dicGlyphSmall;

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

                    var characterPosition = new CharacterPosition(
                        tempCharSizeInfo[charMap.charInformation.charSizeInfoIndex].offsetY,
                        tempCharSizeInfo[charMap.charInformation.charSizeInfoIndex].offsetX);
                    var characterInfo = new CharacterInfo(charMap.charInformation.charWidth, characterPosition);
                    Characters.Add(new FontCharacter2(charMap.codePoint, glyph, characterInfo));
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
            /*
            //Update image
            #region  Compiling and saving new image
            var img = new MemoryStream();
            var i0a = new BitmapInfo(image_0).pixelMap(BitmapInfo.Channel.Alpha);
            var i1a = new BitmapInfo(image_1).pixelMap(BitmapInfo.Channel.Alpha);
            var i2a = new BitmapInfo(image_2).pixelMap(BitmapInfo.Channel.Alpha);

            bmp = new Bitmap(bmp.Width, bmp.Height);
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                    bmp.SetPixel(x, y, Color.FromArgb(255, i0a[x, y], i1a[x, y], i2a[x, y]));

            _xi.Image = bmp;
            _xi.Save(img);
            _xpck.Files[0].FileData = img;
            #endregion

            //Compact charSizeInfo
            var compactCharSizeInfo = new List<XfCharSizeInfo>();
            #region Compacting and updating dictionaries
            foreach (var info in lstCharSizeInfoLarge)
                if (compactCharSizeInfo.Contains(info.Value))
                    dicGlyphLarge[info.Key].char_size = (ushort)(compactCharSizeInfo.FindIndex(c => c.Equals(info.Value)) % 1024 + dicGlyphLarge[info.Key].CharWidth * 1024);
                else
                {
                    dicGlyphLarge[info.Key].char_size = (ushort)(compactCharSizeInfo.Count % 1024 + dicGlyphLarge[info.Key].CharWidth * 1024);
                    compactCharSizeInfo.Add(info.Value);
                }
            foreach (var info in lstCharSizeInfoSmall)
                if (compactCharSizeInfo.Contains(info.Value))
                    dicGlyphSmall[info.Key].char_size = (ushort)(compactCharSizeInfo.FindIndex(c => c.Equals(info.Value)) % 1024 + dicGlyphSmall[info.Key].CharWidth * 1024);
                else
                {
                    dicGlyphSmall[info.Key].char_size = (ushort)(compactCharSizeInfo.Count % 1024 + dicGlyphSmall[info.Key].CharWidth * 1024);
                    compactCharSizeInfo.Add(info.Value);
                }
            #endregion

            //Writing
            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true))
            {
                //Table0
                Header.table0EntryCount = (short)compactCharSizeInfo.Count;
                bw.BaseStream.Position = 0x28;
                bw.WriteMultipleCompressed(compactCharSizeInfo, _t0Comp);
                bw.WriteAlignment(4);

                //Table1
                Header.table1Offset = (short)(bw.BaseStream.Position >> 2);
                Header.table1EntryCount = (short)dicGlyphLarge.Count;
                bw.WriteMultipleCompressed(dicGlyphLarge.Select(d => d.Value), _t1Comp);
                bw.WriteAlignment(4);

                //Table2
                Header.table2Offset = (short)(bw.BaseStream.Position >> 2);
                Header.table2EntryCount = (short)dicGlyphSmall.Count;
                bw.WriteMultipleCompressed(dicGlyphSmall.Select(d => d.Value), _t2Comp);
                bw.WriteAlignment(4);

                //Header
                bw.BaseStream.Position = 0;
                bw.WriteStruct(Header);
            }
            _xpck.Files[1].FileData = ms;

            _xpck.Save(output);*/
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