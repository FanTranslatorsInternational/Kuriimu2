using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace plugin_nintendo.Images
{
    public class Support
    {
        public static Dictionary<int, IColorEncoding> CtrFormat = new Dictionary<int, IColorEncoding>
        {
            [0] = new RGBA(8, 8, 8, 8),
            [1] = new RGBA(8, 8, 8),
            [2] = new RGBA(5, 5, 5, 1),
            [3] = new RGBA(5, 6, 5),
            [4] = new RGBA(4, 4, 4, 4),
            [5] = new LA(8, 8),
            [6] = new RGBA(8, 8, 0),
            [7] = new LA(8, 0),
            [8] = new LA(0, 8),
            [9] = new LA(4, 4),
            [10] = new LA(4, 0),
            [11] = new LA(0, 4),
            [12] = new ETC1(true, true, ByteOrder.LittleEndian, 8),
            [13] = new ETC1(true, true, ByteOrder.LittleEndian, 8)
        };
    }

    public class Header
    {
        [FixedLength(4)]
        public string magic;
        public short version;
        public short texCount;
        public int texSecOffset;
        public int texSecSize;
        public int crc32SecOffset;
        public int texInfoOffset;
    }

    public class CtpkEntry
    {
        public TexEntry texEntry;
        public List<int> dataSizes = new List<int>();
        public string name;
        public HashEntry hash;
        public MipmapEntry mipmapEntry;
    }

    public class TexEntry
    {
        public int nameOffset;
        public int texDataSize;
        public int texOffset;
        public int imageFormat;
        public short width;
        public short height;
        public byte mipLvl;
        public byte type;
        public short zero0;
        public int bitmapSizeOffset;
        public uint timeStamp;
    }

    public class HashEntry
    {
        public uint crc32;
        public int id;
    }

    public class MipmapEntry
    {
        public byte mipmapFormat;
        public byte mipLvl;
        //never used compression specifications?
        public byte compression;
        public byte compMethod;
    }
}
