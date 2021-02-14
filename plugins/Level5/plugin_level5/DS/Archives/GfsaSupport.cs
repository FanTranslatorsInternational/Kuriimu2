using System.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Progress;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_level5.DS.Archives
{
    class GfsaHeader
    {
        [FixedLength(4)]
        public string magic;

        public int directoryOffset;
        public int fileOffset;
        public int unkOffset;
        public int stringOffset;
        public int fileDataOffset;

        public int directoryCount;
        public int fileCount;
        public int decompressedTableSize;   // Summed sizes of decompressed tables
        public int unk4;
    }

    class GfsaDirectoryEntry
    {
        public ushort hash;
        public short fileCount;
        public int fileIndex;
    }

    class GfsaFileEntry
    {
        public ushort hash;
        public ushort offLow;
        public ushort sizeLow;
        public byte offHigh;
        public byte sizeHigh;

        public int Offset
        {
            get => (offLow | (offHigh << 16)) << 2;
            set
            {
                offLow = (ushort)((value >> 2) & 0xFFFF);
                offHigh = (byte)((value >> 18) & 0xFF);
            }
        }

        public int Size
        {
            get => sizeLow | ((sizeHigh & 0xF0) << 12);
            set
            {
                sizeLow = (ushort)(value & 0xFFFF);
                sizeHigh = (byte)((value & 0xF0000) >> 12);
            }
        }
    }

    class GfsaString
    {
        public string Value { get; }

        public ushort Hash { get; }

        public GfsaString(string value, ushort hash)
        {
            Value = value;
            Hash = hash;
        }
    }

    class GfsaArchiveFileInfo : ArchiveFileInfo
    {
        public long CompressedSize => UsesCompression ? GetCompressedStream().Length : FileSize + 4;

        public GfsaFileEntry Entry { get; }

        public GfsaArchiveFileInfo(Stream fileData, string filePath, GfsaFileEntry entry) :
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public GfsaArchiveFileInfo(Stream fileData, string filePath, GfsaFileEntry entry, IKompressionConfiguration configuration, long decompressedSize) :
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }

        public override long SaveFileData(Stream output, bool compress, IProgressContext progress = null)
        {
            if (!UsesCompression)
            {
                var compressionHeader = new[] {
                    (byte)(output.Length << 3),
                    (byte)(output.Length >> 5),
                    (byte)(output.Length >> 13),
                    (byte)(output.Length >> 21) };
                output.Write(compressionHeader, 0, 4);
            }

            var writtenSize = base.SaveFileData(output, compress, progress);

            while (output.Position % 4 > 0) output.WriteByte(0);

            return writtenSize;
        }
    }
}
