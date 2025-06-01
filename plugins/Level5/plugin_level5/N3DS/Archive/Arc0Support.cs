using Komponent.Contract.Aspects;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

#pragma warning disable 649

namespace plugin_level5.N3DS.Archive
{
    struct Arc0Header
    {
        public string magic; // ARC0
        public int directoryEntriesOffset;
        public int directoryHashOffset;
        public int fileEntriesOffset;
        public int nameOffset;
        public int dataOffset;
        public short directoryEntriesCount;
        public short directoryHashCount;
        public int fileEntriesCount;
        public int tableChunkSize;
        public int zero1;

        // Hashes?
        public uint unk2;
        public uint unk3;
        public uint unk4;
        public uint unk5;

        public int directoryCount;
        public int fileCount;
        public uint unk7;
        public int zero2;
    }

    public class Arc0FileEntry
    {
        public uint crc32;  // only filename
        public uint nameOffsetInFolder;
        public uint fileOffset;
        public uint fileSize;
    }

    struct Arc0DirectoryEntry
    {
        public uint crc32;   // directoryName
        public ushort firstDirectoryIndex;
        public short directoryCount;
        public ushort firstFileIndex;
        public short fileCount;
        public int fileNameStartOffset;
        public int directoryNameStartOffset;
    }

    public class Arc0ArchiveFile : ArchiveFile
    {
        public Arc0FileEntry Entry { get; }

        public Arc0ArchiveFile(ArchiveFileInfo fileInfo, Arc0FileEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }

    static class Arc0Support
    {
        public static Guid[]? RetrievePluginMapping(Stream fileStream, string fileName)
        {
            string extension = Path.GetExtension(fileName);

            using var br = new BinaryReaderX(fileStream, true);
            string magic = br.PeekString(4);

            switch (extension)
            {
                case ".xi":
                    return [Guid.Parse("79159dba-3689-448f-8343-167d58a54b2c")];

                case ".xf":
                    return [Guid.Parse("b1b397c4-9a02-4828-b568-39cad733fa3a")];

                case ".xr":
                case ".xc":
                case ".xa":
                case ".xk":
                    if (magic == "XPCK")
                        return [Guid.Parse("de276e88-fb2b-48a6-a55f-d6c14ec60d4f")];

                    goto default;

                default:
                    return null;
            }
        }
    }
}
