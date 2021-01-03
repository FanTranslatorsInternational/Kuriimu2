using System;
using System.Collections.Generic;
using System.Text;
using Komponent.IO.Attributes;
using Kontract.Models.IO;
#pragma warning disable 649

namespace plugin_nintendo.Nitro
{
    class NitroHeader
    {
        [FixedLength(4)]
        public string magic;

        public ByteOrder byteOrder;
        public short unk1;
        public int sectionSize;
        public short headerSize;
        public short sectionCount;
    }
}
