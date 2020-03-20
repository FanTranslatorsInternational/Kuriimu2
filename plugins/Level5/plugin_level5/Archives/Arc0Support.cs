using System.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;

namespace plugin_level5.Archives
{
    class Arc0Header
    {
        [FixedLength(4)]
        public string magic;
        public uint offset1;
        public uint offset2;
        public uint fileEntriesOffset;
        public uint nameOffset;
        public uint dataOffset;
        public short table1Count;
        public short tble2Count;
        public int fileEntriesCount;
        public uint unk1;
        public int zero1;

        //Hashes?
        public uint unk2;
        public uint unk3;
        public uint unk4;
        public uint unk5;

        public uint unk6;
        public int fileCount;
        public uint unk7;
        public int zero2;
    }

    class Arc0FileEntry
    {
        public uint crc32;  // only filename.ToLower()
        public uint nameOffsetInFolder;
        public uint fileOffset;
        public uint fileSize;
    }

    class Arc0ArchiveFileInfo : ArchiveFileInfo
    {
        public Arc0FileEntry Entry { get; }

        public Arc0ArchiveFileInfo(Stream fileData, string filePath, Arc0FileEntry entry) :
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public override void SaveFileData(Stream output, IProgressContext progress)
        {
            FileData.Position = 0;
            FileData.CopyTo(output);

            while (output.Position % 4 != 0)
                output.WriteByte(0);
        }
    }
}
