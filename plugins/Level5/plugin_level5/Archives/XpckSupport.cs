using System;
using System.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;

namespace plugin_level5.Archives
{
    class XpckHeader
    {
        [FixedLength(4)]
        public string magic = "XPCK";

        public byte fc1;
        public byte fc2;

        public ushort infoOffsetUnshifted;
        public ushort nameTableOffsetUnshifted;
        public ushort dataOffsetUnshifted;
        public ushort infoSizeUnshifted;
        public ushort nameTableSizeUnshifted;
        public uint dataSizeUnshifted;

        public ushort FileCount
        {
            get => (ushort)((fc2 & 0xf) << 8 | fc1);
            set
            {
                fc2 = (byte)((fc2 & 0xF0) | ((value >> 8) & 0x0F));
                fc1 = (byte)value;
            }
        }

        public ushort FileInfoOffset
        {
            get => (ushort)(infoOffsetUnshifted << 2);
            set => infoOffsetUnshifted = (ushort)(value >> 2);
        }

        public ushort FilenameTableOffset
        {
            get => (ushort)(nameTableOffsetUnshifted << 2);
            set => nameTableOffsetUnshifted = (ushort)(value >> 2);
        }

        public ushort DataOffset
        {
            get => (ushort)(dataOffsetUnshifted << 2);
            set => dataOffsetUnshifted = (ushort)(value >> 2);
        }

        public ushort FileInfoSize
        {
            get => (ushort)(infoSizeUnshifted << 2);
            set => infoSizeUnshifted = (ushort)(value >> 2);
        }

        public ushort FilenameTableSize
        {
            get => (ushort)(nameTableSizeUnshifted << 2);
            set => nameTableSizeUnshifted = (ushort)(value >> 2);
        }

        public uint DataSize
        {
            get => dataSizeUnshifted << 2;
            set => dataSizeUnshifted = value >> 2;
        }
    }

    class XpckFileInfo
    {
        public uint hash;
        public ushort nameOffset;

        public ushort tmp;
        public ushort tmp2;
        public byte tmpZ;
        public byte tmp2Z;

        public int FileOffset
        {
            get => ((tmpZ << 16) | tmp) << 2;
            set
            {
                tmpZ = (byte)(value >> 18);
                tmp = (ushort)(value >> 2);
            }
        }

        public int FileSize
        {
            get => (tmp2Z << 16) | tmp2;
            set
            {
                tmp2Z = (byte)(value >> 16);
                tmp2 = (ushort)value;
            }
        }
    }

    class XpckArchiveFileInfo : ArchiveFileInfo
    {
        public XpckFileInfo FileEntry;

        public XpckArchiveFileInfo(Stream fileData, string filePath, XpckFileInfo entry) :
            base(fileData, filePath)
        {
            FileEntry = entry;
        }

        public override long SaveFileData(Stream output, bool compress, IProgressContext progress = null)
        {
            var writtenSize = base.SaveFileData(output, compress, progress);

            if (output.Position % 4 > 0)
            {
                while (output.Position % 4 != 0)
                    output.WriteByte(0);
            }
            else
            {
                output.Write(new byte[4], 0, 4);
            }

            return writtenSize;
        }
    }

    static class XpckSupport
    {
        public static Guid[] RetrievePluginMapping(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            switch (extension)
            {
                case ".xi":
                    return new[] { Guid.Parse("898c9151-71bd-4638-8f90-6d34f0a8600c") };

                default:
                    return null;
            }
        }
    }
}
