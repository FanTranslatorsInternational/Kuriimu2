namespace plugin_nintendo.Archives
{
    struct GcDiscHeader
    {
        public GcDiscGameCode gameCode;
        public short makerCode;
        public byte discId;
        public byte version;
        public bool audioStreamingEnabled;
        public byte streamBufferSize;
        public byte[] padding;
        public uint magic; // 0xc2339f3d
        public string gameName;
        public int dhOffset;
        public int dbgLoadAddress;
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

    struct GcDiscGameCode
    {
        public byte consoleId;
        public short gameCode;
        public byte countryCode;
    }

    struct GcAppLoader
    {
        public string date;
        public byte[] padding;
        public int entryPoint;
        public int size;
        public int trailerSize;
    }
}
