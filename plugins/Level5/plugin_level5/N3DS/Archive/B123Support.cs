using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

#pragma warning disable 649

namespace plugin_level5.N3DS.Archive
{
    struct B123Header
    {
        public string magic; // B123
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

        //Hashes?
        public uint unk2;
        public uint unk3;
        public uint unk4;
        public uint unk5;

        public int directoryCount;
        public int fileCount;
        public uint unk7;
        public int zero2;
    }

    public class B123FileEntry
    {
        // TODO: Hashes of files to lower?
        public uint crc32;  // only filename.ToLower()
        public uint nameOffsetInFolder;
        public uint fileOffset;
        public uint fileSize;
    }

    struct B123DirectoryEntry
    {
        // TODO: Hashes of files to lower?
        public uint crc32;  // directoryName.ToLower()
        public short fileCount;
        public short directoryCount;
        public int fileNameStartOffset;
        public int firstFileIndex;
        public int firstDirectoryIndex;
        public int directoryNameStartOffset;
    }

    public class B123ArchiveFile : ArchiveFile
    {
        public B123FileEntry Entry { get; }

        public B123ArchiveFile(ArchiveFileInfo fileInfo, B123FileEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
