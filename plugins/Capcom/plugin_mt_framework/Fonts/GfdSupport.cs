using System.Buffers.Binary;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace plugin_mt_framework.Fonts
{
    public class GfdHeaderv1
    {
        public string Magic;
        public uint Version;
        public int unk0;
        public int unk1;
        public int unk2;
        public int FontSize;
        public int FontTexCount;
        public int CharCount;
        public int FCount;
        public float BaseLine;
        public float DescentLine;
    }

    public class GfdHeaderv2
    {
        public string Magic;
        public uint Version;
        public int unk0;
        public int unk1;
        public int unk2;
        public int FontSize;
        public int FontTexCount;
        public int CharCount;
        public int unk3;
        public int FCount;
        public float MaxCharacterWidth;
        public float MaxCharacterHeight;
        public float BaseLine;
        public float DescentLine;
    }

    public class GfdEntryv1
    {
        public uint codePoint;
        public uint tmp1;
        public uint tmp2;
        public byte charWidth;
        public byte posX;
        public byte posY;
        public byte padding;

        public byte ImageId
        {
            get => (byte)tmp1;
            set => tmp1 = (tmp1 & 0xFFFFFF00) | value;
        }

        public short GlyphPositionX
        {
            get => (short)((tmp1 >> 8) & 0xFFF);
            set => tmp1 = (tmp1 & 0xFFF000FF) | (uint)((value & 0xFFF) << 8);
        }

        public short GlyphPositionY
        {
            get => (short)((tmp1 >> 20) & 0xFFF);
            set => tmp1 = (tmp1 & 0x000FFFFF) | (uint)((value & 0xFFF) << 20);
        }

        public short GlyphWidth
        {
            get => (short)((tmp2 >> 8) & 0xFFF);
            set => tmp2 = (tmp2 & 0xFFF000FF) | (uint)((value & 0xFFF) << 8);
        }

        public short GlyphHeight
        {
            get => (short)((tmp2 >> 20) & 0xFFF);
            set => tmp2 = (tmp2 & 0x000FFFFF) | (uint)((value & 0xFFF) << 20);
        }
    }

    public class GfdEntryv2
    {
        public uint codePoint;
        public uint tmp1;
        public uint tmp2;
        public uint tmp3 = 0x14000000;
        public byte posX;
        public byte posY;
        public ushort endMark = 0xFFFF;

        public byte ImageId
        {
            get => (byte)tmp1;
            set => tmp1 = (tmp1 & 0xFFFFFF00) | value;
        }

        public short GlyphPositionX
        {
            get => (short)((tmp1 >> 8) & 0xFFF);
            set => tmp1 = (tmp1 & 0xFFF000FF) | (uint)((value & 0xFFF) << 8);
        }

        public short GlyphPositionY
        {
            get => (short)((tmp1 >> 20) & 0xFFF);
            set => tmp1 = (tmp1 & 0x000FFFFF) | (uint)((value & 0xFFF) << 20);
        }

        public short GlyphWidth
        {
            get => (short)(tmp2 & 0xFFF);
            set => tmp2 = (tmp2 & 0xFFFFF000) | (uint)(value & 0xFFF);
        }

        public short GlyphHeight
        {
            get => (short)((tmp2 >> 12) & 0xFFF);
            set => tmp2 = (tmp2 & 0xFF000FFF) | (uint)((value & 0xFFF) << 12);
        }

        public short CharWidth
        {
            get => (short)(tmp3 & 0xFFF);
            set => tmp3 = (tmp3 & 0xFFFFF000) | (uint)(value & 0xFFF);
        }

        public short CharHeight
        {
            get => (short)((tmp3 >> 12) & 0xFFF);
            set => tmp3 = (tmp3 & 0xFF000FFF) | (uint)((value & 0xFFF) << 12);
        }
    }

    enum FontVersion
    {
        V1,
        V2
    }

    class GfdSupport
    {
        public static FontVersion PeekVersion(Stream input)
        {
            if (input.Length < 8)
                throw new InvalidOperationException("File needs to be at least 8 bytes.");

            long bkPos = input.Position;
            input.Position = 4;

            var buffer = new byte[4];
            _ = input.Read(buffer);

            input.Position = bkPos;

            uint rawVersion = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            return rawVersion switch
            {
                0x00010C06 => FontVersion.V1,
                0x00010F06 => FontVersion.V2,
                _ => throw new InvalidOperationException($"Font version 0x{rawVersion:X8} not supported.")
            };
        }
    }
}
