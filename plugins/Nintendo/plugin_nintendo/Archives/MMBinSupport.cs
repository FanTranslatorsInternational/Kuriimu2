using System;
using System.Collections.Generic;
using System.Text;
using Komponent.IO.Attributes;

namespace plugin_nintendo.Archives
{
    class MMBinHeader
    {
        public int tableSize;
        public short resourceCount;
        public short unk1;
        public int unk2;
    }

    class MMBinResourceEntry
    {
        [FixedLength(0x24)]
        public string resourceName;
        public int offset;
        public int metaSize;
        public int ctpkSize;
        [FixedLength(0xC)]
        public byte[] padding = new byte[0xC];
    }
}
