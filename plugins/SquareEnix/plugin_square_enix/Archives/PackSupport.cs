using System.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;
using Kontract.Models.IO;
#pragma warning disable 649

namespace plugin_square_enix.Archives
{
    class PackHeader
    {
        [FixedLength(4)]
        public string magic;
        public int size;
        public short unk1;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public ByteOrder byteOrder = ByteOrder.LittleEndian;
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

    class PackArchiveFileInfo : ArchiveFileInfo
    {
        public FileEntry Entry { get; }

        public PackArchiveFileInfo(Stream fileData, string filePath, FileEntry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }

        public override long SaveFileData(Stream output, bool compress, IProgressContext progress = null)
        {
            var writtenSize = base.SaveFileData(output, compress, progress);

            while (output.Position % 4 != 0)
                output.WriteByte(0);

            return writtenSize;
        }
    }

    class PackSupport
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
