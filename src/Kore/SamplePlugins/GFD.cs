using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace Kore.SamplePlugins
{
    public class GFD
    {
        public Header Header;
        public List<float> HeaderF;
        public string Name;
        public List<GfdCharacter> Characters;
        public ByteOrder ByteOrder = ByteOrder.LittleEndian;
        public BitOrder BitOrder = BitOrder.MSBFirst;

        public GFD(Stream input)
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
                Header = br.ReadStruct<Header>();
                HeaderF = br.ReadMultiple<float>(Header.FCount);

                // Name
                br.ReadInt32();
                Name = br.ReadCStringASCII();

                // Characters
                Characters = br.ReadMultiple<CharacterInfo>(Header.CharacterCount).Select(ci => new GfdCharacter
                {
                    Character = ci.Block0,

                    TextureID = (int)ci.Block1.TextureIndex,
                    GlyphX = (int)ci.Block1.GlyphX,
                    GlyphY = (int)ci.Block1.GlyphY,

                    Block2Trailer = (int)ci.Block2.Block2Trailer,
                    GlyphWidth = (int)ci.Block2.GlyphWidth,
                    GlyphHeight = (int)ci.Block2.GlyphHeight,

                    Block3 = ci.Block3
                }).ToList();
            }
        }

        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, ByteOrder, BitOrder))
            {
                // Header
                Header.CharacterCount = Characters.Count;
                bw.WriteStruct(Header);
                foreach (var f in HeaderF)
                    bw.Write(f);

                // Name
                bw.Write(Name.Length);
                bw.WriteASCII(Name);
                bw.Write((byte)0);

                // Characters
                bw.WriteMultiple(Characters.Select(c => new CharacterInfo
                {
                    Block0 = c.Character,

                    Block1 = new Block1
                    {
                        GlyphY = c.GlyphY,
                        GlyphX = c.GlyphX,
                        TextureIndex = c.TextureID
                    },

                    Block2 = new Block2
                    {
                        GlyphHeight = c.GlyphHeight,
                        GlyphWidth = c.GlyphWidth,
                        Block2Trailer = c.Block2Trailer
                    },

                    Block3 = c.Block3
                }));
            }
        }
    }

    public class Header
    {
        [FieldLength(4)]
        public string Magic;
        public uint Version;
        public int Unk0;
        public int Unk1;
        public int Unk2;
        public int FontSize;
        public int FontTexCount;
        public int CharacterCount;
        public int FCount;
        public float BaseLine;
        public float DescentLine;
    }

    public class CharacterInfo
    {
        public uint Block0;
        public Block1 Block1;
        public Block2 Block2;
        public uint Block3;
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

    public class GfdCharacter : FontCharacter, ICloneable
    {
        /// <summary>
        /// Trailing 8 bits in block2 that are unknown
        /// </summary>
        [FormField(typeof(int), "Block 2 Trailer")]
        public int Block2Trailer { get; set; }

        /// <summary>
        /// Unknown character metadata.
        /// </summary>
        [FormField(typeof(uint), "Block 3")]
        public uint Block3 { get; set; }

        /// <summary>
        /// Allows cloning of GfdCharcaters,
        /// </summary>
        /// <returns>A cloned GfdCharacter.</returns>
        public override object Clone() => new GfdCharacter
        {
            Character = Character,
            TextureID = TextureID,
            GlyphX = GlyphX,
            GlyphY = GlyphY,
            GlyphWidth = GlyphWidth,
            GlyphHeight = GlyphHeight,
            Block2Trailer = Block2Trailer,
            Block3 = Block3
        };
    }
}
