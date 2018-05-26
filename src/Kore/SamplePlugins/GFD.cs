using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using Komponent.IO;
using Kontract.Interface;

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
                var nameSize = br.ReadInt32();
                Name = br.ReadCStringASCII();

                // Characters
                Characters = br.ReadMultiple<CharacterInfo>(Header.CharacterCount)
                    .Select(ci => new GfdCharacter
                    {
                        Character = ci.block0,

                        TextureIndex = (int)ci.block1.TextureIndex,
                        GlyphX = (int)ci.block1.GlyphX,
                        GlyphY = (int)ci.block1.GlyphY,

                        Block2Trailer = (int)ci.block2.Block2Trailer,
                        GlyphWidth = (int)ci.block2.GlyphWidth,
                        GlyphHeight = (int)ci.block2.GlyphHeight,

                        Block3 = ci.block3
                    }).ToList(); //new List<GfdCharacter>();
                /*
                for (var i = 0; i < Header.CharacterCount; i++)
                {
                    var block0 = br.ReadUInt32();
                    var block1 = br.ReadUInt32();
                    var block2 = br.ReadUInt32();
                    var block3 = br.ReadUInt32();

                    if (ByteOrder == ByteOrder.LittleEndian)
                    {
                        Characters.Add(new GfdCharacter
                        {
                            Character = block0,
                            TextureIndex = (int)((block1 >> 0) & 0xFF),
                            GlyphX = (int)((block1 >> 8) & 0xFFF),
                            GlyphY = (int)((block1 >> 20) & 0xFFF),

                            Block2Trailer = (int)((block2 >> 0) & 0xFF),
                            GlyphWidth = (int)((block2 >> 8) & 0xFFF),
                            GlyphHeight = (int)((block2 >> 20) & 0xFFF),

                            Block3 = block3
                        });
                    }
                    else
                    {
                        Characters.Add(new GfdCharacter
                        {
                            Character = block0,
                            TextureIndex = (int)((block1 >> 24) & 0xFF),
                            GlyphX = (int)((block1 >> 12) & 0xFFF),
                            GlyphY = (int)((block1 >> 0) & 0xFFF),

                            Block2Trailer = (int)((block2 >> 24) & 0xFF),
                            GlyphWidth = (int)((block2 >> 12) & 0xFFF),
                            GlyphHeight = (int)((block2 >> 0) & 0xFFF),

                            Block3 = block3
                        });
                    }
                }*/
            }
        }

        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, ByteOrder))
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
                bw.Write(Characters.Select(c => new CharacterInfo
                {
                    block0 = c.Character,

                    block1 = new Block1
                    {
                        GlyphY = c.GlyphY,
                        GlyphX = c.GlyphX,
                        TextureIndex = c.TextureIndex
                    },

                    block2 = new Block2
                    {
                        GlyphHeight = c.GlyphHeight,
                        GlyphWidth = c.GlyphWidth,
                        Block2Trailer = c.Block2Trailer
                    },

                    block3 = c.Block3
                }));

                /*for (var i = 0; i < Header.CharacterCount; i++)
                {
                    var chr = Characters[i];

                    var block0 = chr.Character;
                    uint block1 = 0;
                    uint block2 = 0;
                    var block3 = chr.Block3;

                    if (ByteOrder == ByteOrder.LittleEndian)
                    {
                        block1 = (uint)(chr.TextureIndex | (chr.GlyphX << 8) | (chr.GlyphY << 20));
                        block2 = (uint)(chr.Block2Trailer | chr.GlyphWidth << 8 | (chr.GlyphHeight << 20));
                    }
                    else
                    {
                        block1 = (uint)(chr.TextureIndex << 24 | (chr.GlyphX << 12) | chr.GlyphY);
                        block2 = (uint)(chr.Block2Trailer << 24 | chr.GlyphWidth << 12 | chr.GlyphHeight);
                    }

                    bw.Write(block0);
                    bw.Write(block1);
                    bw.Write(block2);
                    bw.Write(block3);
                }*/
            }
        }
    }

    #region Structs
    public class Header
    {
        [Length(4)]
        public string Magic;
        public uint Version;
        public int unk0;
        public int unk1;
        public int unk2;
        public int FontSize;
        public int FontTexCount;
        public int CharacterCount;
        public int FCount;
        public float BaseLine;
        public float DescentLine;
    }

    public class CharacterInfo
    {
        public uint block0;
        public Block1 block1;
        public Block2 block2;
        public uint block3;
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
    #endregion

    public class GfdCharacter : FontCharacter
    {
        /// <summary>
        /// Trailing 8 bits that block2 is ignoring
        /// </summary>
        public int Block2Trailer { get; set; }

        /// <summary>
        /// Unknown character metadata.
        /// </summary>
        public uint Block3 { get; set; }
    }
}
