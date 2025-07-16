using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_spike_chunsoft.Archives
{
    class SpcHeader
    {
        public string magic = "CPS.";
        public int zero0;
        public long unk1 = -1;
    }

    class SpcEntry
    {
        public short flag;
        public short unk1;
        public int compSize;
        public int decompSize;
        public int nameLength;
        public byte[] zero0 = new byte[0x10];
        public string name;
    }

    class SpcArchiveFile : ArchiveFile
    {
        public SpcEntry Entry { get; }

        public SpcArchiveFile(ArchiveFileInfo fileInfo, SpcEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
