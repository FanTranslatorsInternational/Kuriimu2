using System.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;

namespace plugin_level5.Archives
{
    class ArcvHeader
    {
        [FixedLength(4)]
        public string magic = "ARCV";
        public int fileCount;
        public int fileSize;
    }

    class ArcvFileInfo
    {
        public int offset;
        public int size;
        public uint hash;
    }

    class ArcvArchiveFileInfo : ArchiveFileInfo
    {
        public ArcvFileInfo Entry { get; }

        public ArcvArchiveFileInfo(Stream fileData, string filePath, ArcvFileInfo entry) :
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public override void SaveFileData(Stream output, IProgressContext progress)
        {
            FileData.Position = 0;
            FileData.CopyTo(output);

            while (output.Position % 0x7F != 0)
                output.WriteByte(0xAC);
        }
    }
}
