using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Image;

namespace Kore.SamplePlugins
{
    public class GFDv2
    {
        public FileHeader Header;
        public List<float> HeaderF;
        public string Name;
        public List<GFDv2Character> Characters;
        public List<Bitmap> Textures;

        public ByteOrder ByteOrder = ByteOrder.LittleEndian;
        public BitOrder BitOrder = BitOrder.MSBFirst;

        private string _sourceFile;

        #region MT Tex Adapter

        private CompositionContainer _container;

        [ImportMany(typeof(IMtFrameworkTextureAdapter))]
        private List<IMtFrameworkTextureAdapter> _texAdapters;

        #endregion

        public GFDv2()
        {
            Header = new FileHeader();
            HeaderF = new List<float>();
            Name = string.Empty;
            Characters = new List<GFDv2Character>();
            Textures = new List<Bitmap>();

            Plugins.ComposePlugins(this, _container);
            Kore.ComposeSamplePlugins(this, _container);
        }

        public GFDv2(FileStream input)
        {
            _sourceFile = input.Name;

            Plugins.ComposePlugins(this, _container);
            Kore.ComposeSamplePlugins(this, _container);

            using (var br = new BinaryReaderX(input))
            {
                // Set endianess
                if (br.PeekString() == "\0DFG")
                {
                    br.ByteOrder = ByteOrder = ByteOrder.BigEndian;
                    br.BitOrder = BitOrder = BitOrder.LSBFirst;
                }

                // Header
                Header = br.ReadStruct<FileHeader>();
                HeaderF = br.ReadMultiple<float>(Header.FCount);

                // Name
                br.ReadInt32();
                Name = br.ReadCStringASCII();

                // Characters
                Characters = br.ReadMultiple<CharacterInfo>(Header.CharacterCount).Select(ci => new GFDv2Character
                {
                    Character = ci.Character,

                    TextureID = (int)ci.Block1.TextureIndex,
                    GlyphX = (int)ci.Block1.GlyphX,
                    GlyphY = (int)ci.Block1.GlyphY,

                    Block2Trailer = (int)ci.Block2.Block2Trailer,
                    GlyphWidth = (int)ci.Block2.GlyphWidth,
                    GlyphHeight = (int)ci.Block2.GlyphHeight,

                    CharacterWidth = (int)ci.Block3.CharacterWidth,
                    CharacterHeight = (int)ci.Block3.CharacterHeight,

                    XAdjust = ci.XAdjust,
                    YAdjust = ci.YAdjust
                }).ToList();

                // Textures
                Textures = new List<Bitmap>();
                for (var i = 0; i < Header.FontTexCount; i++)
                {
                    var texAdapter = _texAdapters.Where(adapter => adapter is IIdentifyFiles).FirstOrDefault(adapter => ((IIdentifyFiles)adapter).Identify(GetTexName(_sourceFile, i)));
                    if (texAdapter == null) continue;
                    //TODO
                    //((ILoadFiles)texAdapter).Load(GetTexName(_sourceFile, i));
                    Textures.Add(((IImageAdapter)texAdapter).BitmapInfos[0].Bitmaps[0]);
                }
            }
        }

        public void Save(FileStream output)
        {
            using (var bw = new BinaryWriterX(output, ByteOrder, BitOrder))
            {
                // Header
                Header.Magic = ByteOrder == ByteOrder.LittleEndian ? "GFD\0" : "\0DFG";
                Header.CharacterCount = Characters.Count;
                Header.FontTexCount = Textures.Count;
                bw.WriteStruct(Header);
                foreach (var f in HeaderF)
                    bw.Write(f);

                // Name
                bw.Write(Name.Length);
                bw.WriteString(Name, Encoding.ASCII, false, false);
                bw.Write((byte)0);

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
                        Block2Trailer = ci.Block2Trailer,
                        GlyphHeight = ci.GlyphHeight,
                        GlyphWidth = ci.GlyphWidth
                    },

                    Block3 = new Block3
                    {
                        CharacterHeight = ci.CharacterHeight,
                        CharacterWidth = ci.CharacterWidth
                    },

                    XAdjust = (byte)ci.XAdjust,
                    YAdjust = (byte)ci.YAdjust
                }));

                // Textures
                for (var i = 0; i < Header.FontTexCount; i++)
                {
                    var texAdapter = _texAdapters.Where(adapter => adapter is IIdentifyFiles).FirstOrDefault(adapter => ((IIdentifyFiles)adapter).Identify(GetTexName(_sourceFile, i)));
                    if (texAdapter == null) continue;
                    //TODO
                    //((ILoadFiles)texAdapter).Load(GetTexName(_sourceFile, i));
                    ((IImageAdapter)texAdapter).BitmapInfos[0].Bitmaps[0] = Textures[i];
                    ((ISaveFiles)texAdapter).Save(GetTexName(output.Name, i));
                }

                _sourceFile = output.Name;
            }
        }

        private string GetTexName(string filename, int textureIndex)
        {
            var dName = Path.GetDirectoryName(filename);
            var fName = Name.Split('\\').Last() + "_" + textureIndex.ToString("00");

            switch (Header.Suffix)
            {
                case 0x1:
                    fName += "_ID";
                    break;
                case 0x3:
                    fName += "_ID_HQ";
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
            public int Unknown1;
            public int FCount;
            public float MaxCharWidth;
            public float MaxCharHeight;

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

            public byte XAdjust;
            public byte YAdjust;
            public ushort Trailer;

            public CharacterInfo()
            {
                Block1 = new Block1();
                Block2 = new Block2();
                Block3 = new Block3();
                XAdjust = 0;
                YAdjust = 0;
                Trailer = 0xFFFF;
            }
        }

        [BitFieldInfo(BlockSize = 32)]
        public class Block1
        {
            [BitField(12)]
            public long GlyphY;
            [BitField(12)]
            public long GlyphX;
            [BitField(8)]
            public long TextureIndex;
        }

        [BitFieldInfo(BlockSize = 32)]
        public class Block2
        {
            [BitField(8)]
            public long Block2Trailer;
            [BitField(12)]
            public long GlyphHeight;
            [BitField(12)]
            public long GlyphWidth;
        }

        [BitFieldInfo(BlockSize = 32)]
        public class Block3
        {
            [BitField(8)]
            public long MaxCharacterHeight = 0x14;
            [BitField(12)]
            public long CharacterHeight;
            [BitField(12)]
            public long CharacterWidth;
        }
    }

    public class GFDv2Character : FontCharacter
    {
        /// <summary>
        /// Trailing 8 bits in block2 that are unknown
        /// </summary>
        [FormField(typeof(int), "Block 2 Trailer")]
        public int Block2Trailer { get; set; }

        /// <summary>
        /// Character width.
        /// </summary>
        [FormField(typeof(int), "Character Width")]
        public int CharacterWidth { get; set; }

        /// <summary>
        /// Character height.
        /// </summary>
        [FormField(typeof(int), "Character Height")]
        public int CharacterHeight { get; set; }

        /// <summary>
        /// X adjustment.
        /// </summary>
        [FormField(typeof(int), "X Adjust")]
        public int XAdjust { get; set; }

        /// <summary>
        /// Y adjustment.
        /// </summary>
        [FormField(typeof(int), "Y Adjust")]
        public int YAdjust { get; set; }

        /// <summary>
        /// Allows cloning of GfdCharcaters,
        /// </summary>
        /// <returns>A cloned GfdCharacter.</returns>
        public override object Clone() => new GFDv2Character
        {
            Character = Character,
            TextureID = TextureID,
            GlyphX = GlyphX,
            GlyphY = GlyphY,
            GlyphWidth = GlyphWidth,
            GlyphHeight = GlyphHeight,
            Block2Trailer = Block2Trailer,
            CharacterWidth = CharacterWidth,
            CharacterHeight = CharacterHeight,
            XAdjust = XAdjust,
            YAdjust = YAdjust
        };
    }
}
