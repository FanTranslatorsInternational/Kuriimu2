using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_square_enix.Archives
{
    class PackHeader
    {
        public string magic;
        public int size;
        public short unk1;
        public ushort byteOrder;
        public short fileCount;
        public short headerSize;
        public int unk2;
        public int unk3;
    }

    class FileEntry
    {
        public short fileStart;
        public short unk2;
        public uint fileSize;
    }

    class PackArchiveFile : ArchiveFile
    {
        public FileEntry Entry { get; }

        public PackArchiveFile(ArchiveFileInfo fileInfo, FileEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }

    static class PackSupport
    {
        public static int GetAlignment(string extension)
        {
            switch (extension)
            {
                case ".bch":
                case ".ptcl":
                case ".arc":
                    return 0x80;
            }

            return 1;
        }
    }
}
