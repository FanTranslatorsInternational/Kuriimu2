using Komponent.IO.Attributes;

namespace plugin_nintendo.Archives
{
    class GcDiscHeader
    {
        public GcDiscGameCode gameCode;
        public short makerCode;
        public byte discId;
        public byte version;
        public bool audioStreamingEnabled;
        public byte streamBufferSize;
        [FixedLength(0x12)] 
        public byte[] padding;
        public uint magic = 0xc2339f3d;
        [FixedLength(0x3e0)] 
        public string gameName;
        public int dhOffset;
        public int dbgLoadAddress;
        [FixedLength(0x18)]
        public byte[] unused1;
        public int execOffset;
        public int fstOffset;
        public int fstSize;
        public int fstMaxSize;  // For multi disc games
        public int userPosition;
        public int userLength;
        public int unk1;
        public int unused2;
    }

    class GcDiscGameCode
    {
        public byte consoleId;
        public short gameCode;
        public byte countryCode;
    }

    class GcAppLoader
    {
        [FixedLength(0xA)] 
        public string date;
        [FixedLength(6)]
        public byte[] padding;
        public int entryPoint;
        public int size;
        public int trailerSize;
    }
}
