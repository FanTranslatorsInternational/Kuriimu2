using System;
using System.IO;
using Komponent.IO.Attributes;

namespace plugin_level5.DS.Archives
{
    class Lpc2Header
    {
        [FixedLength(4)]
        public string magic = "LPC2";
        public int fileCount;
        public int headerSize;
        public int fileSize;

        public int fileEntryOffset;
        public int nameOffset;
        public int dataOffset;
    }

    class Lpc2FileEntry
    {
        public int nameOffset;
        public int fileOffset;
        public int fileSize;
    }

    class Lpc2Support
    {
        public static Guid[] RetrievePluginMapping(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            switch (extension)
            {
                case ".cimg":
                    return new[] { Guid.Parse("169acf3f-ccc8-4193-b32c-84b44c0f6f68") };

                default:
                    return null;
            }
        }
    }
}
