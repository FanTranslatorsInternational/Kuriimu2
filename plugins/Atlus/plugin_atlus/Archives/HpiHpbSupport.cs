using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Progress;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kryptography.Hash;

namespace Atlus.Archives
{
    class HpiHeader
    {
        [FixedLength(4)]
        public string magic = "HPIH";
        public int zero0;
        public int headerSize = 0x10;  //without magic and zero0
        public int zero1;
        public short zero2;
        public short hashCount;
        public int entryCount;
    }

    class HpiHashEntry
    {
        public short entryOffset;
        public short entryCount;
    }

    class HpiFileEntry
    {
        public int stringOffset;
        public int offset;
        public int compSize;
        public int decompSize;
    }

    class HpiHpbArchiveFileInfo : ArchiveFileInfo
    {
        public HpiFileEntry Entry { get; }

        public HpiHpbArchiveFileInfo(Stream fileData, string filePath, HpiFileEntry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }

        public HpiHpbArchiveFileInfo(Stream fileData, string filePath, HpiFileEntry entry, IKompressionConfiguration configuration, long decompressedSize) :
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }

        public override long SaveFileData(Stream output, bool compress, IProgressContext progress = null)
        {
            var position = output.Position;

            var offset = 0;
            if (UsesCompression)
                offset = 0x20;

            output.Position += offset;
            var writtenSize = base.SaveFileData(output, compress, progress);

            // Padding
            while (output.Position % 4 != 0)
                output.WriteByte(0);

            if (!UsesCompression)
                return writtenSize + offset;

            var bkPos = output.Position;
            using var bw = new BinaryWriterX(output, true);

            output.Position = position;
            bw.WriteString("ACMP", Encoding.ASCII, false, false);
            bw.Write((int)writtenSize);
            bw.Write(0x20);
            bw.Write(0);
            bw.Write((int)FileSize);
            bw.Write(0x01234567);
            bw.Write(0x01234567);
            bw.Write(0x01234567);

            output.Position = bkPos;
            return writtenSize + offset;
        }
    }

    class HpiHpbSupport
    {
        private static readonly Encoding Sjis = Encoding.GetEncoding("SJIS");
        private static readonly SimpleHash SimpleHash = new SimpleHash(0x25);

        public static string PeekString(Stream input, int length)
        {
            var bkPos = input.Position;

            using var br = new BinaryReaderX(input, true);
            var result = br.ReadString(length);

            input.Position = bkPos;

            return result;
        }

        public static uint CreateHash(string input)
        {
            var bytes = Sjis.GetBytes(input);

            var hash = SimpleHash.Compute(bytes);
            return (uint)((hash[0] << 24) | (hash[1] << 16) | (hash[2] << 8) | hash[3]);
        }
    }

    class SlashFirstStringComparer : IComparer<UPath>
    {
        private static readonly IComparer<UPath> DefaultComparer = Comparer<UPath>.Default;

        private readonly IComparer<UPath> _comparer;

        public SlashFirstStringComparer() : this(DefaultComparer)
        {
        }

        public SlashFirstStringComparer(IComparer<UPath> stringComparer)
        {
            _comparer = stringComparer;
        }

        public int Compare(UPath x, UPath y)
        {
            if (x == y)
                return 0;

            var xFull = x.FullName;
            var yFull = y.FullName;

            // Find first difference
            var index = -1;
            for (var i = 0; i < Math.Min(xFull.Length, yFull.Length); i++)
                if (xFull[i] != yFull[i])
                {
                    index = i;
                    break;
                }

            // If no difference was found, use default comparer, instead of returning 0
            // This blocks false equality based on the default string comparer desired
            if (index == -1)
                return _comparer.Compare(x, y);

            if (xFull[index] == '.' && yFull[index] == '/')
                return 1;
            if (xFull[index] == '/' && yFull[index] == '.')
                return -1;

            return _comparer.Compare(x, y);
        }
    }
}
