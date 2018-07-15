using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace Kore.SamplePlugins
{
    public class GFDv1 : IFormatConverter<GFDv1, GFDv2>
    {
        public FileHeader Header;
        public List<float> HeaderF;
        public string Name;
        public List<GFDv1Character> Characters;

        public ByteOrder ByteOrder = ByteOrder.LittleEndian;
        public BitOrder BitOrder = BitOrder.MSBFirst;

        public GFDv1()
        {
            Header = new FileHeader();
            HeaderF = new List<float>();
            Name = string.Empty;
            Characters = new List<GFDv1Character>();
        }

        public GFDv1(Stream input)
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
                Characters = br.ReadMultiple<CharacterInfo>(Header.CharacterCount).Select(ci => new GFDv1Character
                {
                    Character = ci.Character,

                    GlyphX = (int)ci.Block1.GlyphX,
                    GlyphY = (int)ci.Block1.GlyphY,
                    TextureID = (int)ci.Block1.TextureIndex,

                    GlyphHeight = (int)ci.Block2.GlyphHeight,
                    GlyphWidth = (int)ci.Block2.GlyphWidth,
                    Block2Trailer = (int)ci.Block2.Block2Trailer,

                    Block3Trailer = (int)ci.Block3.Block3Trailer,
                    CharacterUnknown = (int)ci.Block3.CharacterUnknown,
                    CharacterKerning = (int)ci.Block3.CharacterKerning
                }).ToList();
            }
        }

        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, ByteOrder, BitOrder))
            {
                // Header
                Header.Magic = ByteOrder == ByteOrder.LittleEndian ? "GFD\0" : "\0DFG";
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
                        GlyphHeight = ci.GlyphHeight,
                        GlyphWidth = ci.GlyphWidth,
                        Block2Trailer = ci.Block2Trailer
                    },

                    Block3 = new Block3
                    {
                        Block3Trailer = ci.Block3Trailer,
                        CharacterUnknown = ci.CharacterUnknown,
                        CharacterKerning = ci.CharacterKerning
                    }
                }));
            }
        }

        // Conversion
        public GFDv2 ConvertTo(GFDv1 inFormat)
        {
            throw new System.NotImplementedException();
        }

        public static implicit operator GFDv2(GFDv1 source)
        {
            throw new System.NotImplementedException();
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

        [BitFieldInfo(BlockSize = 32)]
        public struct Block1
        {
            [BitField(12)]
            public long GlyphY;
            [BitField(12)]
            public long GlyphX;
            [BitField(8)]
            public long TextureIndex;
        }

        [BitFieldInfo(BlockSize = 32)]
        public struct Block2
        {
            [BitField(12)]
            public long GlyphHeight;
            [BitField(12)]
            public long GlyphWidth;
            [BitField(8)]
            public long Block2Trailer;
        }

        [BitFieldInfo(BlockSize = 32)]
        public struct Block3
        {
            [BitField(8)]
            public long Block3Trailer;
            [BitField(12)]
            public long CharacterUnknown;
            [BitField(12)]
            public long CharacterKerning;
        }
    }

    public class GFDv1Character : FontCharacter
    {
        /// <summary>
        /// Trailing 8 bits in block2 that are unknown
        /// </summary>
        [FormField(typeof(int), "Block 2 Trailer")]
        public int Block2Trailer { get; set; }

        /// <summary>
        /// Character kerning.
        /// </summary>
        [FormField(typeof(int), "Kerning")]
        public int CharacterKerning { get; set; }

        /// <summary>
        /// Character unknown.
        /// </summary>
        [FormField(typeof(int), "Unknown")]
        public int CharacterUnknown { get; set; }

        /// <summary>
        /// Trailing 8 bits in block3 that are unknown
        /// </summary>
        [FormField(typeof(int), "Block 3 Trailer")]
        public int Block3Trailer { get; set; }

        /// <summary>
        /// Allows cloning of GfdCharcaters,
        /// </summary>
        /// <returns>A cloned GfdCharacter.</returns>
        public override object Clone() => new GFDv1Character
        {
            Character = Character,
            TextureID = TextureID,
            GlyphX = GlyphX,
            GlyphY = GlyphY,
            GlyphWidth = GlyphWidth,
            GlyphHeight = GlyphHeight,
            Block2Trailer = Block2Trailer,
            CharacterKerning = CharacterKerning,
            CharacterUnknown = CharacterUnknown,
            Block3Trailer = Block3Trailer
        };
    }
}
