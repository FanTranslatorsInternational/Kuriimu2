using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_tri_ace.Archives
{
    class PackHeader
    {
        public string magic = "P@CK";
        public short version = 3;
        public short fileCount;
    }

    class PackFileEntry
    {
        public int offset;
        public int fileType; // 2 = P@CK; 0x400 = mpak8
        public int unk0; // Maybe ID?
        public int zero0;
    }

    class PackArchiveFile : ArchiveFile
    {
        public PackFileEntry Entry { get; }

        public PackArchiveFile(ArchiveFileInfo fileInfo, PackFileEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }

    static class PackSupport
    {
        public static string DetermineExtension(int fileType)
        {
            switch (fileType)
            {
                case 0x2:
                    return ".pack";

                case 0x20:
                case 0x30:
                case 0x40:
                    return ".cgfx";

                case 0x400:
                    return ".mpak8";

                default:
                    return ".bin";
            }
        }

        public static Guid[]? RetrievePluginMapping(int fileType)
        {
            switch (fileType)
            {
                case 0x2:
                    return [Guid.Parse("8c81d937-e1a8-42e6-910a-d9911a6a93af")];

                default:
                    return null;
            }
        }
    }
}
