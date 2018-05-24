using System.Collections.Generic;
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
        public List<FontCharacter> Characters;

        public GFD(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                // Set endianess
                if (br.PeekString() == "\0DFG")
                    br.ByteOrder = ByteOrder.BigEndian;

                // Header
                Header = br.ReadStruct<Header>();
                HeaderF = br.ReadMultiple<float>(Header.FCount);

                // Name
                var nameSize = br.ReadInt32();
                Name = br.ReadCStringA();

                // Character Entries
                Characters = new List<FontCharacter>();
                for (var i = 0; i < Header.CharacterCount; i++)
                {
                    var block0 = br.ReadUInt32();
                    var block1 = br.ReadUInt32();
                    var block2 = br.ReadUInt32();
                    br.ReadUInt32();

                    if (br.ByteOrder == ByteOrder.LittleEndian)
                    {
                        Characters.Add(new FontCharacter
                        {
                            Character = block0,
                            TextureIndex = (int)((block1 >> 0) & 0xFF),
                            GlyphX = (int)((block1 >> 8) & 0xFFF),
                            GlyphY = (int)((block1 >> 20) & 0xFFF),

                            GlyphWidth = (int)((block2 >> 8) & 0xFFF),
                            GlyphHeight = (int)((block2 >> 20) & 0xFFF)
                        });
                    }
                    else
                    {
                        Characters.Add(new FontCharacter
                        {
                            Character = block0,
                            TextureIndex = (int)((block1 >> 24) & 0xFF),
                            GlyphX = (int)((block1 >> 12) & 0xFFF),
                            GlyphY = (int)((block1 >> 0) & 0xFFF),

                            GlyphWidth = (int)((block2 >> 12) & 0xFFF),
                            GlyphHeight = (int)((block2 >> 0) & 0xFFF)
                        });
                    }
                }
            }
        }

        public void Save(Stream output)
        {
            // Soon~
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Header
    {
        public Magic Magic;
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
}
