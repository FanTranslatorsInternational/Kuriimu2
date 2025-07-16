namespace plugin_level5.NDS.Archive
{
    struct Lpc2Header
    {
        public string magic; // LPC2
        public int fileCount;
        public int headerSize;
        public int fileSize;
        public int fileEntryOffset;
        public int nameOffset;
        public int dataOffset;
    }

    struct Lpc2FileEntry
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
