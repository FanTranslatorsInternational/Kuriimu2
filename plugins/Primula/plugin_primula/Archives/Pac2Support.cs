using System.IO;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Models.Archive;

namespace plugin_primula.Archives
{
    class Pac2Header
    {
        [FixedLength(12)]
        public string magic = "GAMEDAT PAC2";
        public int fileCount;
    }

    class Pac2Entry
    {
        public int Position;
        public int Size;
    }
}
