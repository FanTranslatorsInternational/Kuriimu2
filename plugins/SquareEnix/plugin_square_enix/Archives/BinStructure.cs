using System;
using System.Collections.Generic;
using System.Text;
using Komponent.IO.Attributes;

namespace plugin_square_enix.Archives
{
    public class Binheader
    {
        public int magic;
        public int fileCount;
        public int fileSize;
        [FixedLength(5)]
        public int[] unknowns;
        //public int unk1;
        //public int unk2;
        //public int unk3;
        //public int unk4;
        //public int unk5;
    }
    public class BinTableEntry
    {
        public int offset;
        public int fileSize;
    }
}
