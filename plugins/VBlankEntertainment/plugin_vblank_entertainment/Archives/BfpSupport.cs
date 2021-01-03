using System;
using System.Collections.Generic;
using System.Text;
using Komponent.IO.Attributes;
#pragma warning disable 649

namespace plugin_vblank_entertainment.Archives
{
    class BfpHeader
    {
        [FixedLength(4)]
        public string magic;

        public int entryCount;
        public int unk1;
        public int unk2;
    }

    class BfpFileEntry
    {
        public uint hash;
        public int offset;
        public int decompSize;
    }

    class BfpBucketFileEntry
    {
        public int offset;
        public int decompSize;
    }
}
