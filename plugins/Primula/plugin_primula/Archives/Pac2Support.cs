namespace plugin_primula.Archives
{
    class Pac2Header
    {
        public string magic = "GAMEDAT PAC2";
        public int fileCount;
    }

    class Pac2Entry
    {
        public int Position;
        public int Size;
    }
}
