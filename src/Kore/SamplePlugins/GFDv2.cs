using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace Kore.SamplePlugins
{
    public class GFDv2
    {
        public FileHeader Header;
        public List<float> HeaderF;
        public string Name;
        public List<GFDv2Character> Characters;

        public ByteOrder ByteOrder = ByteOrder.LittleEndian;
        public BitOrder BitOrder = BitOrder.MSBFirst;

        public GFDv2()
        {
            Header = new FileHeader();
            HeaderF = new List<float>();
            Name = string.Empty;
            Characters = new List<GFDv2Character>();
        }

        public GFDv2(Stream input)
        {
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
                var cis = br.ReadMultiple<CharacterInfo>(Header.CharacterCount);
                Characters = cis.Select(ci => new GFDv2Character
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
            }
        }

        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, ByteOrder, BitOrder))
            {
                // Header
                Header.Magic = ByteOrder == ByteOrder.LittleEndian ? "GFD" : "\0DFG";
                Header.CharacterCount = Characters.Count;
                bw.WriteStruct(Header);
                foreach (var f in HeaderF)
                    bw.Write(f);

                // Name
                bw.Write(Name.Length);
                bw.WriteASCII(Name);
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
            }
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
            public long Block2Trailer = 0;
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
        /// 
        /// </summary>
        [FormField(typeof(int), "X Adjust")]
        public int XAdjust { get; set; }

        /// <summary>
        /// 
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
