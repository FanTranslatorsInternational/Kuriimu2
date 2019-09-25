using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Image;
using plugin_mt_framework.TEX;

namespace plugin_mt_framework.GFDv1
{
    public class GFDv1
    {
        public FileHeader Header;
        public List<float> HeaderF;
        public string Name;
        public List<GFDv1Character> Characters;
        public List<Bitmap> Textures;

        public ByteOrder ByteOrder = ByteOrder.LittleEndian;
        public BitOrder BitOrder = BitOrder.MSBFirst;

        private string _sourceFile;

        private List<IMtFrameworkTextureAdapter> _texAdapters;

        public GFDv1()
        {
            Header = new FileHeader();
            HeaderF = new List<float>();
            Name = string.Empty;
            Characters = new List<GFDv1Character>();
            Textures = new List<Bitmap>();

            if (_texAdapters == null || _texAdapters.Count == 0)
                _texAdapters = PluginLoader.Instance.GetAdapters<IMtFrameworkTextureAdapter>();
        }

        public GFDv1(StreamInfo input)
        {
            _sourceFile = input.FileName;

            if (_texAdapters == null || _texAdapters.Count == 0)
                _texAdapters = PluginLoader.Instance.GetAdapters<IMtFrameworkTextureAdapter>();

            using (var br = new BinaryReaderX(input.FileData))
            {
                // Set endianess
                if (br.PeekString() == "\0DFG")
                {
                    br.ByteOrder = ByteOrder = ByteOrder.BigEndian;
                    br.BitOrder = BitOrder = BitOrder.LSBFirst;
                }

                // Header
                Header = br.ReadType<FileHeader>();
                HeaderF = br.ReadMultiple<float>(Header.FCount);

                // Name
                br.ReadInt32();
                Name = br.ReadCStringASCII();

                // Characters
                Characters = br.ReadMultiple<CharacterInfo>(Header.CharacterCount).Select(ci => new GFDv1Character
                {
                    Character = ci.Character,

                    WidthInfo = new CharWidthInfo
                    {
                        CharWidth = (int)ci.Block2.GlyphWidth,
                        GlyphWidth = (int)ci.Block2.GlyphWidth,
                        Left = (int)ci.Block2.XAdjust
                    },

                    GlyphX = (int)ci.Block1.GlyphX,
                    GlyphY = (int)ci.Block1.GlyphY,
                    TextureID = (int)ci.Block1.TextureIndex,

                    GlyphWidth = (int)ci.Block2.GlyphWidth,
                    GlyphHeight = (int)ci.Block2.GlyphHeight,
                    XAdjust = (int)ci.Block2.XAdjust,

                    IsSpace = ci.Block3.IsSpace > 0,
                    Superscript = ci.Block3.Superscript > 0,
                    CharacterWidth = (int)ci.Block3.CharacterWidth
                }).ToList();

                // Textures
                Textures = new List<Bitmap>();
                for (var i = 0; i < Header.FontTexCount; i++)
                {
                    var streamInfo = new StreamInfo { FileData = File.OpenRead(GetTexName(_sourceFile, i)), FileName = GetTexName(_sourceFile, i) };
                    var texAdapter = _texAdapters.Where(adapter => adapter is IIdentifyFiles).FirstOrDefault(adapter => ((IIdentifyFiles)adapter).Identify(streamInfo, null));
                    streamInfo.FileData.Position = 0;
                    if (texAdapter == null) continue;
                    ((ILoadFiles)texAdapter).Load(streamInfo, null);
                    Textures.Add(((IImageAdapter)texAdapter).BitmapInfos[0].Image);
                }

                // Glyphs
                foreach (var c in Characters)
                {
                    var width = c.WidthInfo.GlyphWidth;
                    var height = c.GlyphHeight;

                    var glyph = new Bitmap(Math.Max(width, 1), Math.Max(height, 1), PixelFormat.Format32bppArgb);
                    var gfx = Graphics.FromImage(glyph);
                    gfx.DrawImage(Textures[c.TextureID],
                        new[] {
                            new PointF(0, 0),
                            new PointF(width, 0),
                            new PointF(0, height)
                        },
                        new RectangleF(c.GlyphX, c.GlyphY, width, height),
                        GraphicsUnit.Pixel);
                    c.Glyph = glyph;
                }
            }
        }

        public void Save(StreamInfo output)
        {
            using (var bw = new BinaryWriterX(output.FileData, ByteOrder, BitOrder))
            {
                // Header
                Header.Magic = ByteOrder == ByteOrder.LittleEndian ? "GFD\0" : "\0DFG";
                Header.CharacterCount = Characters.Count;
                Header.FontTexCount = Textures.Count;
                bw.WriteType(Header);
                foreach (var f in HeaderF)
                    bw.Write(f);

                // Name
                bw.Write(Name.Length);
                bw.WriteString(Name, Encoding.ASCII, false);

                // Characters
                bw.WriteMultiple(Characters.Select(ci => new CharacterInfo
                {
                    Character = ci.Character,

                    Block1 = new Block1
                    {
                        GlyphY = ci.GlyphY,
                        GlyphX = ci.GlyphX,
                        TextureIndex = ci.TextureID
                    },

                    Block2 = new Block2
                    {
                        GlyphHeight = ci.GlyphHeight,
                        GlyphWidth = ci.GlyphWidth,
                        XAdjust = ci.XAdjust
                    },

                    Block3 = new Block3
                    {
                        IsSpace = ci.IsSpace ? 1 : 0,
                        Superscript = ci.Superscript ? 1 : 0,
                        CharacterWidth = ci.CharacterWidth
                    }
                }));

                // Textures
                for (var i = 0; i < Header.FontTexCount; i++)
                {
                    var streamInfo = new StreamInfo { FileData = File.OpenRead(GetTexName(_sourceFile, i)), FileName = GetTexName(_sourceFile, i) };
                    var texAdapter = _texAdapters.Where(adapter => adapter is IIdentifyFiles).FirstOrDefault(adapter => ((IIdentifyFiles)adapter).Identify(streamInfo, null));
                    streamInfo.FileData.Position = 0;
                    if (texAdapter == null) continue;
                    ((ILoadFiles)texAdapter).Load(streamInfo, null);
                    ((IImageAdapter)texAdapter).BitmapInfos[0].Image = Textures[i];
                    ((ISaveFiles)texAdapter).Save(new StreamInfo { FileData = File.Create(GetTexName(output.FileName, i)), FileName = GetTexName(output.FileName, i) }, null);
                }

                _sourceFile = output.FileName;
            }
        }

        private string GetTexName(string filename, int textureIndex)
        {
            var dName = Path.GetDirectoryName(filename) ?? string.Empty;
            var fName = Name.Split('\\').Last() + "_" + textureIndex.ToString("00");

            switch (Header.Suffix)
            {
                case 0x1:
                    fName += "_ID";
                    break;
                case 0x3:
                    fName += "_ID_HQ";
                    break;
                case 0x5:
                    fName += "_GSM_HQ_NOMIP";
                    break;
                case 0x6:
                    fName += "_AM_NOMIP";
                    break;
            }
            fName += ".tex";

            return Path.Combine(dName, fName);
        }

        // Support
        public class FileHeader
        {
            [FixedLength(4)]
            public string Magic;
            public uint Version;

            /// <summary>
            /// IsDynamic, InsertSpace, EvenLayout
            /// </summary>
            public int HeaderBlock1;

            /// <summary>
            /// This is texture suffix id (as in NOMIP, etc.)
            /// 0x0 and anything greater than 0x6 means no suffix
            /// </summary>
            public int Suffix;

            public int FontType;
            public int FontSize;
            public int FontTexCount;
            public int CharacterCount;
            public int FCount;

            /// <summary>
            /// Internally called MaxAscent
            /// </summary>
            public float Baseline;

            /// <summary>
            /// Internally called MaxDescent
            /// </summary>
            public float DescentLine;
        }

        public class CharacterInfo
        {
            public uint Character;
            public Block1 Block1;
            public Block2 Block2;
            public Block3 Block3;
        }

        [BitFieldInfo(BlockSize = 4)]
        public struct Block1
        {
            [BitField(12)]
            public long GlyphY;
            [BitField(12)]
            public long GlyphX;
            [BitField(8)]
            public long TextureIndex;
        }

        [BitFieldInfo(BlockSize = 4)]
        public struct Block2
        {
            [BitField(12)]
            public long GlyphHeight;
            [BitField(12)]
            public long GlyphWidth;
            [BitField(8)]
            public long XAdjust;
        }

        [BitFieldInfo(BlockSize = 4)]
        public struct Block3
        {
            [BitField(14)]
            public long IsSpace;
            [BitField(6)]
            public long Superscript;
            [BitField(12)]
            public long CharacterWidth;
        }
    }

    public class GFDv1Character : FontCharacter2
    {
        /// <summary>
        /// Character width.
        /// </summary>
        [FormField(typeof(int), "Character Width")]
        public int CharacterWidth { get; set; }

        [FormField(typeof(int), "Texture ID")]
        public virtual int TextureID { get; set; } = 0;

        [FormField(typeof(int), "X")]
        public virtual int GlyphX { get; set; } = 0;

        [FormField(typeof(int), "Y")]
        public virtual int GlyphY { get; set; } = 0;

        /// <summary>
        /// Trailing 8 bits in block2 that are unknown
        /// </summary>
        [FormField(typeof(int), "X Adjust")]
        public int XAdjust { get; set; }

        /// <summary>
        /// Determines whether the character is a space.
        /// </summary>
        [FormField(typeof(bool), "Is Space")]
        public bool IsSpace { get; set; }

        /// <summary>
        /// Determines whether the character is superscript.
        /// </summary>
        [FormField(typeof(bool), "Superscript")]
        public bool Superscript { get; set; }

        /// <summary>
        /// Allows cloning of GfdCharcaters,
        /// </summary>
        /// <returns>A cloned GfdCharacter.</returns>
        public override object Clone() => new GFDv1Character
        {
            Character = Character,
            GlyphWidth = GlyphWidth,
            GlyphHeight = GlyphHeight,
            XAdjust = XAdjust,
            CharacterWidth = CharacterWidth,
            Superscript = Superscript,
            IsSpace = IsSpace
        };
    }
}
