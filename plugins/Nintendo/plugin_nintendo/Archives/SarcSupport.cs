using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    class SarcHeader
    {
        [FixedLength(4)]
        public string magic = "SARC";
        public short headerSize = 0x14;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public ByteOrder byteOrder;
        public int fileSize;
        public int dataOffset;
        public int unk1;
    }

    class SfatHeader
    {
        [FixedLength(4)]
        public string magic = "SFAT";
        public short headerSize = 0xC;
        public short entryCount;
        public uint hashMultiplier = 0x65;
    }

    class SfatEntry
    {
        public uint nameHash;
        public uint fntFlagOffset;  // 0x01000000 set -> uses SFNT
        public int startOffset;
        public int endOffset;

        public int Flags
        {
            get => (int)(fntFlagOffset >> 16);
            set => fntFlagOffset = (fntFlagOffset & 0x0000FFFF) | ((uint)value << 16);
        }

        public int FntOffset
        {
            get => (int)(fntFlagOffset & 0xFFFF) << 2;
            set => fntFlagOffset = (fntFlagOffset & 0xFFFF0000) | ((uint)(value >> 2) & 0xFFFF);
        }
    }

    class SfntHeader
    {
        [FixedLength(4)]
        public string magic = "SFNT";
        public short headerSize = 0x8;
        public short zero0;
    }

    class SarcArchiveFileInfo : ArchiveFileInfo
    {
        public string Type { get; private set; }

        public SfatEntry Entry { get; }

        public SarcArchiveFileInfo(Stream fileData, string filePath, string magic, SfatEntry entry) : base(fileData, filePath)
        {
            Type = magic;
            Entry = entry;
        }

        public override void SetFileData(Stream fileData)
        {
            base.SetFileData(fileData);

            Type = SarcSupport.DetermineMagic(fileData);
            fileData.Position = 0;
        }
    }

    class SarcSupport
    {
        private const int DefaultAlignmentCompressed = 0x80;
        private const int DefaultAlignment = 0x4;

        private static readonly IDictionary<string, int> AlignmentLittleEndianCompressed = new Dictionary<string, int>
        {
            ["MsgS"] = 0x4,
            ["MsgF"] = 0x4,
            ["SMDH"] = 0x4,
            ["YB\x1\0"] = 0x4
        };

        private static readonly IDictionary<string, int> AlignmentLittleEndian = new Dictionary<string, int>
        {
            ["FFNT"] = 0x2000,
            ["CFNT"] = 0x2000,
            ["CFNU"] = 0x2000
        };

        private static readonly IDictionary<string, int> AlignmentBigEndian = new Dictionary<string, int>
        {
            ["FLAN"] = 0x4,
            ["FLYT"] = 0x4
        };

        private static readonly IDictionary<string, string> Extensions = new Dictionary<string, string>
        {
            ["CTPK"] = ".ctpk",
            ["BCH\0"] = ".bch",
            ["CFNT"] = ".bcfnt",
            ["CFNU"] = ".bcfnt",
            ["CLIM"] = ".bclim",
            ["DVLB"] = ".shbin",
            ["FFNT"] = ".bffnt",
            ["FLAN"] = ".bflan",
            ["FLIM"] = ".bflim",
            ["FLYT"] = ".bflyt",
            ["MsgF"] = ".msbf",
            ["MsgS"] = ".msbt",
            ["SMDH"] = ".icn",
            ["SPBD"] = ".ptcl",
            ["YB\x1\0"] = ".byaml"
        };

        public static string DetermineMagic(Stream input)
        {
            using var br = new BinaryReaderX(input, true);
            var magic = br.ReadString(4);

            if (Extensions.ContainsKey(magic))
                return magic;

            input.Position = input.Length - 0x28;
            magic = br.ReadString(4);

            if (Extensions.ContainsKey(magic))
                return magic;

            return null;
        }

        public static string DetermineExtension(string magic)
        {
            if (magic == null)
                return ".bin";

            return Extensions.ContainsKey(magic) ? Extensions[magic] : ".bin";
        }

        public static int DetermineAlignment(SarcArchiveFileInfo file, ByteOrder byteOrder, bool isCompressed)
        {
            // Special cases
            using var br = new BinaryReaderX(file.GetFileData().Result, true, byteOrder);
            switch (file.Type)
            {
                case "CLIM":
                    br.BaseStream.Position = br.BaseStream.Length - 6;
                    return br.ReadInt16();

                case "FLIM":
                    br.BaseStream.Position = br.BaseStream.Length - 8;
                    return br.ReadInt16();
            }

            // Set alignments
            var alignments = byteOrder == ByteOrder.LittleEndian ?
                isCompressed ? AlignmentLittleEndianCompressed : AlignmentLittleEndian :
                AlignmentBigEndian;

            if (string.IsNullOrEmpty(file.Type) || !alignments.ContainsKey(file.Type))
                return isCompressed ? DefaultAlignmentCompressed : DefaultAlignment;

            return alignments[file.Type];
        }
    }
}
