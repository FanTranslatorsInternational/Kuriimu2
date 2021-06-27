using System;
using System.Collections.Generic;
using System.Text;
using Komponent.IO.Attributes;

namespace plugin_koei_tecmo.Archives
{
    class CraeHeader
    {
        [FixedLength(4)]
        public string magic;
        public int unk1;
        public int dataSize;
        public int entryOffset;
        public int dataOffset;
        public int fileCount;
        public int unk2;
    }

    class CraeEntry
    {
        public int offset;
        public int size;
        [FixedLength(0x30)]
        public string name;
    }

    class CraeSupport
    {
    }
}
