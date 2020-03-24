using System.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;

namespace plugin_level5.Archives
{
    // TODO: Research unk1
    class XfsaHeader
    {
        [FixedLength(4)]
        public string magic = "XFSA";

        public int directoryEntriesOffset;
        public int directoryHashOffset;
        public int fileEntriesOffset;
        public int nameOffset;
        public int dataOffset;

        public short directoryEntriesCount;
        public short directoryHashCount;
        public int fileEntriesCount;
        public int unk1;
    }

    class XfsaFileEntry
    {
        public uint crc32;  // filename.ToLower()
        public int tmp1;  //offset combined with an unknown value, offset is last 24 bits with 4bit left-shift
        public int tmp2;   //size combined with an unknown value, size is last 20 bits

        public int FileOffset
        {
            get => tmp1 & 0x03FFFFFF;
            set => tmp1 = (tmp1 & ~0x03FFFFFF) | (value & 0x03FFFFFF);
        }

        public int FileSize
        {
            get => tmp2 & 0x007FFFF;
            set => tmp2 = (tmp2 & ~0x007FFFFF) | (value & 0x007FFFFF);
        }

        public int NameOffset
        {
            get => (tmp1 >> 26 << 9) | (tmp2 >> 23);
            set
            {
                tmp1 = (tmp1 & ~0x03FFFFFF) | (value >> 9 << 26);
                tmp2 = (tmp2 & ~0x007FFFFF) | (value << 23);
            }
        }
    }

    class XfsaDirectoryEntry
    {
        public uint crc32;  // directoryName.ToLower()
        public int tmp1;
        public short firstFileIndex;
        public short firstDirectoryIndex;
        public int tmp2;

        public int FileNameStartOffset
        {
            get => tmp1 >> 14;
            set => tmp1 = (tmp1 & 0x3FFF) | (value << 14);
        }

        public int DirectoryNameOffset
        {
            get => tmp2 >> 14;
            set => tmp2 = (tmp2 & 0x3FFF) | (value << 14);
        }

        public int FileCount
        {
            get => tmp1 & 0x3FFF;
            set => tmp1 = (tmp1 & ~0x3FFF) | (value & 0x3FFF);
        }

        public int DirectoryCount
        {
            get => tmp2 & 0x3FFF;
            set => tmp2 = (tmp2 & ~0x3FFF) | (value & 0x3FFF);
        }
    }

    class XfsaArchiveFileInfo : ArchiveFileInfo
    {
        public XfsaFileEntry Entry { get; }

        public XfsaArchiveFileInfo(Stream fileData, string filePath, XfsaFileEntry entry) :
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public override void SaveFileData(Stream output, IProgressContext progress)
        {
            base.SaveFileData(output,progress);

            output.Position = output.Length;
            while (output.Position % 4 != 0)
                output.WriteByte(0);
        }
    }
}
