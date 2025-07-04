using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_konami.Archives
{
    class NlpHeader
    {
        public int unk1;
        public int fileBlockOffset;
        public int unk2;
        public int unk3;

        public int entryCount;
        public int blockEntriesOffset;  // relative to header
        public int unk4;
        public int unk5;

        public int unkCount;
        public int unkOffset;           // relative to header
    }

    class NlpMeta
    {
        public string magic = "\0\0\0\0";

        public int zero0;
        public int size;        // size of file; decomp size for PAK
        public int dataStart;   // Only needed for PAK
        public int unk2 = 0x08000000; // Only set for PAK?
    }

    class NlpBlockOffsetHeader
    {
        public int zero0;
        public int entryCount;
        public int offset;              // for some reason relative to header
    }

    class NlpBlockOffset
    {
        public int metaId;
        public int offset;
    }

    class NlpArchiveFile : ArchiveFile
    {
        public NlpMeta Meta { get; }

        public int Id { get; }

        public NlpArchiveFile(ArchiveFileInfo fileInfo, NlpMeta meta, int id) : base(fileInfo)
        {
            Meta = meta;
            Id = id;
        }
    }

    class NlpSupport
    {
        public static string DetermineExtension(NlpMeta meta)
        {
            switch (meta.magic)
            {
                case "PAK ":
                    return ".pack";

                case "FNTB":
                    return ".fntb";

                case "TRB ":
                    return ".trb";

                case "ICN ":
                    return ".icn";

                case "STBL":
                    return ".stbl";

                case "FBIN":
                    return ".fbin";

                case "TBIN":
                    return ".tbin";

                case "NGBI":
                    return ".ngbi";

                default:
                    return ".bin";
            }
        }
    }
}
