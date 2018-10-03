using Komponent.IO;

namespace plugin_valkyria_chronicles
{
    public sealed class PacketHeader
    {
        [FixedLength(4)]
        public string Magic;
        public int PacketSize;
        public int HeaderSize;
        public int Flags;
    }

    public sealed class PacketHeaderX
    {
        [FixedLength(4)]
        public string Magic;
        public int PacketSize;
        public int HeaderSize;
        public int Flags;
        public int Depth;
        public int DataSize;
        public int Unk2;
        public int Unk3;
    }
}
