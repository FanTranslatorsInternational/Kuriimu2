using System;
using Komponent.IO.Attributes;

namespace plugin_spike_chunsoft.Archives
{
    class SpcHeader
    {
        [FixedLength(4)] 
        public string magic = "CPS.";
        public int zero0;
        public long unk1 = -1;
    }

    [Alignment(0x10)]
    class SpcEntry
    {
        public short flag;
        public short unk1 = 4;
        public int compSize;
        public int decompSize;
        public int nameLength;

        [FixedLength(0x10)] 
        public byte[] zero0 = new Byte[0x10];

        [VariableLength(nameof(nameLength))]
        public string name;
    }
}
