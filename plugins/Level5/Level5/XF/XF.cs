using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Komponent.IO;
using Kore.XFont.Archive;
using Kore.XFont.Image;
using Kore.XFont.Compression;
using Kontract.Interfaces;
using Kontract.Interfaces.Font;
using Komponent.IO.Attributes;

namespace Kore.XFont
{
    public class XF
    {
        [DebuggerDisplay("[{offset_x}, {offset_y}, {glyph_width}, {glyph_height}]")]
        public class CharSizeInfo
        {
            public sbyte offset_x;
            public sbyte offset_y;
            public byte glyph_width;
            public byte glyph_height;

            public override bool Equals(object obj)
            {
                var csi = (CharSizeInfo)obj;
                return offset_x == csi.offset_x && offset_y == csi.offset_y && glyph_width == csi.glyph_width && glyph_height == csi.glyph_height;
            }
        }

        //TODO: due to brx and bwx changes, this structure is wrong; CharWidth and CharSizeInfoIndex are the only things that need BlockSize 2
        [BitFieldInfo(BlockSize = 2)]
        [DebuggerDisplay("[{code_point}] {ColorChannel}:{ImageOffsetX}:{ImageOffsetY}")]
        public class CharacterMap
        {
            public char code_point;
            [BitField(6)]
            public long CharWidth;
            [BitField(10)]
            public long CharSizeInfoIndex;
            [BitField(14)]
            public long ImageOffsetY;
            [BitField(14)]
            public long ImageOffsetX;
            [BitField(4)]
            public long ColorChannel;

            /*public int CharSizeInfoIndex => char_size % 1024;
            public int CharWidth => char_size / 1024;
            public int ColorChannel => image_offset % 16;
            public int ImageOffsetX => image_offset / 16 % 16384;
            public int ImageOffsetY => image_offset / 16 / 16384;*/
        }

        public class XFHeader
        {
            [FixedLength(8)]
            public string magic;
            public int BaseLine;
            public short DescentLine;
            public short unk3;
            public short unk4;
            public short unk5;
            public long zero0;

            public short table0Offset;
            public short table0EntryCount;
            public short table1Offset;
            public short table1EntryCount;
            public short table2Offset;
            public short table2EntryCount;
        }

        public Dictionary<char, CharSizeInfo> lstCharSizeInfoLarge;
        public Dictionary<char, CharSizeInfo> lstCharSizeInfoSmall;
        public Dictionary<char, CharacterMap> dicGlyphLarge;
        public Dictionary<char, CharacterMap> dicGlyphSmall;

        Bitmap bmp;
        /*public Bitmap image_0;
        public Bitmap image_1;
        public Bitmap image_2;*/

        XPCK xpck;
        IMGC xi;
        public XFHeader Header;

        Level5.Method t0Comp;
        Level5.Method t1Comp;
        Level5.Method t2Comp;

        public List<XFCharacter> Characters { get; set; }
        public List<Bitmap> Textures { get; set; }

        public XF(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                //load files
                xpck = new XPCK(input);

                //get xi image to bmp
                xi = new IMGC(xpck.Files[0].FileData);
                bmp = xi.Image;

                //decompress fnt.bin
                var tempCharSizeInfo = new List<CharSizeInfo>();
                var largeChars = new List<CharacterMap>();
                var smallChars = new List<CharacterMap>();
                using (var fntR = new BinaryReaderX(xpck.Files[1].FileData, true))
                {
                    Header = fntR.ReadType<XFHeader>();

                    fntR.BaseStream.Position = Header.table0Offset << 2;
                    t0Comp = (Level5.Method)(fntR.ReadInt32() & 0x7);
                    fntR.BaseStream.Position -= 4;
                    tempCharSizeInfo = new BinaryReaderX(new MemoryStream(Level5.Decompress(fntR.BaseStream))).ReadMultiple<CharSizeInfo>(Header.table0EntryCount);

                    fntR.BaseStream.Position = Header.table1Offset << 2;
                    t1Comp = (Level5.Method)(fntR.ReadInt32() & 0x7);
                    fntR.BaseStream.Position -= 4;
                    largeChars = new BinaryReaderX(new MemoryStream(Level5.Decompress(fntR.BaseStream))).ReadMultiple<CharacterMap>(Header.table1EntryCount);

                    fntR.BaseStream.Position = Header.table2Offset << 2;
                    t2Comp = (Level5.Method)(fntR.ReadInt32() & 0x7);
                    fntR.BaseStream.Position -= 4;
                    smallChars = new BinaryReaderX(new MemoryStream(Level5.Decompress(fntR.BaseStream))).ReadMultiple<CharacterMap>(Header.table2EntryCount);
                }

                Textures = new List<Bitmap>();
                var bmpInfo = new BitmapInfo(bmp);
                Textures.Add(bmpInfo.CreateChannelBitmap(BitmapInfo.Channel.Red));
                Textures.Add(bmpInfo.CreateChannelBitmap(BitmapInfo.Channel.Green));
                Textures.Add(bmpInfo.CreateChannelBitmap(BitmapInfo.Channel.Blue));

                //Add Characters
                Characters = new List<XFCharacter>();
                foreach (var glyph in largeChars)
                {
                    var newChar = new XFCharacter
                    {
                        Character = glyph.code_point,
                        TextureID = (int)glyph.ColorChannel,
                        GlyphX = (int)glyph.ImageOffsetX,
                        GlyphY = (int)glyph.ImageOffsetY,
                        GlyphWidth = tempCharSizeInfo[(int)glyph.CharSizeInfoIndex].glyph_width,
                        GlyphHeight = tempCharSizeInfo[(int)glyph.CharSizeInfoIndex].glyph_height,
                        CharacterWidth = (int)glyph.CharWidth,
                        OffsetX = tempCharSizeInfo[(int)glyph.CharSizeInfoIndex].offset_x,
                        OffsetY = tempCharSizeInfo[(int)glyph.CharSizeInfoIndex].offset_y,
                    };
                    Characters.Add(newChar);
                }
                /*foreach (var glyph in smallChars)
                    Characters.Add(new XFCharacter
                    {
                        Character = glyph.code_point,
                        TextureID = (int)glyph.ColorChannel,
                        GlyphX = (int)glyph.ImageOffsetX,
                        GlyphY = (int)glyph.ImageOffsetY,
                        GlyphWidth = tempCharSizeInfo[(int)glyph.CharSizeInfoIndex].glyph_width,
                        GlyphHeight = tempCharSizeInfo[(int)glyph.CharSizeInfoIndex].glyph_height,
                        CharacterWidth = (int)glyph.CharWidth,
                        OffsetX = tempCharSizeInfo[(int)glyph.CharSizeInfoIndex].offset_x,
                        OffsetY = tempCharSizeInfo[(int)glyph.CharSizeInfoIndex].offset_y,
                    });*/

                //Add Textures
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

            xi.Image = bmp;
            xi.Save(img);
            xpck.Files[0].FileData = img;
            #endregion

            //Compact charSizeInfo
            var compactCharSizeInfo = new List<CharSizeInfo>();
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
                bw.WriteMultipleCompressed(compactCharSizeInfo, t0Comp);
                bw.WriteAlignment(4);

                //Table1
                Header.table1Offset = (short)(bw.BaseStream.Position >> 2);
                Header.table1EntryCount = (short)dicGlyphLarge.Count;
                bw.WriteMultipleCompressed(dicGlyphLarge.Select(d => d.Value), t1Comp);
                bw.WriteAlignment(4);

                //Table2
                Header.table2Offset = (short)(bw.BaseStream.Position >> 2);
                Header.table2EntryCount = (short)dicGlyphSmall.Count;
                bw.WriteMultipleCompressed(dicGlyphSmall.Select(d => d.Value), t2Comp);
                bw.WriteAlignment(4);

                //Header
                bw.BaseStream.Position = 0;
                bw.WriteStruct(Header);
            }
            xpck.Files[1].FileData = ms;

            xpck.Save(output);*/
        }
    }

    public class XFCharacter : FontCharacter
    {
        public int CharacterWidth;
        public int OffsetX;
        public int OffsetY;
    }

    public static class XFExtensions
    {
        public static void WriteMultipleCompressed<T>(this BinaryWriterX bw, IEnumerable<T> list, Level5.Method comp)
        {
            var ms = new MemoryStream();
            using (var bwIntern = new BinaryWriterX(ms, true))
                foreach (var t in list)
                    bwIntern.WriteType(t);
            bw.Write(Level5.Compress(ms, comp));
        }
    }

    /// <summary>
    /// Class that expose method to get pixel map from a specific channel
    /// </summary>
    public class BitmapInfo
    {
        private Bitmap m_bitmap;
        public enum Channel
        {
            Red,
            Green,
            Blue,
            Alpha
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bitmap">Instance of bitmap object</param>
        public BitmapInfo(Bitmap bitmap)
        {
            this.m_bitmap = bitmap;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">Path of bitmap picture</param>
        public BitmapInfo(string path)
        {
            this.m_bitmap = new Bitmap(path);
        }

        /// <summary>
        /// Extract a map of picture (2D Array) for a specific channel
        /// </summary>
        /// <param name="channel_index">Channel to extract</param>
        /// <returns>Pixel Map</returns>
        public byte[,] pixelMap(Channel channel_index)
        {
            int size = this.m_bitmap.Width * this.m_bitmap.Height;
            int picture_width = this.m_bitmap.Width;
            int picture_height = this.m_bitmap.Height;
            byte[,] pixels_map = new byte[picture_width, picture_height];

            for (int i = 0; i < picture_height; i++)
            {
                for (int j = 0; j < picture_width; j++)
                {
                    Color color = this.m_bitmap.GetPixel(j, i);
                    byte color_intensity = 0;
                    switch (channel_index)
                    {
                        case Channel.Red:
                            color_intensity = color.R;
                            break;
                        case Channel.Green:
                            color_intensity = color.G;
                            break;
                        case Channel.Blue:
                            color_intensity = color.B;
                            break;
                        case Channel.Alpha:
                            color_intensity = color.A;
                            break;
                    }

                    pixels_map[j, i] = color_intensity;
                }
            }

            return pixels_map;
        }

        public Bitmap CreateChannelBitmap(Channel channel_index)
        {
            var channelMap = pixelMap(channel_index);

            var bmp = new Bitmap(m_bitmap.Width, m_bitmap.Height);
            for (int i = 0; i < m_bitmap.Height; i++)
                for (int j = 0; j < m_bitmap.Width; j++)
                    bmp.SetPixel(j, i, Color.FromArgb(channelMap[j, i], 255, 255, 255));

            return bmp;
        }
    }
}