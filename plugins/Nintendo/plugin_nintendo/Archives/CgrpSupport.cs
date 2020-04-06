using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    [Alignment(0x10)]
    class CgrpHeader
    {
        [FixedLength(4)]
        public string magic = "CGRP";
        public ByteOrder byteOrder = ByteOrder.LittleEndian;
        public short headerSize;
        public uint version = 0x01010000;
        public uint fileSize;
        public int partitionCount;

        [VariableLength("partitionCount")]
        public CgrpHeaderPartition[] partitions;
    }

    class CgrpHeaderPartition
    {
        public int partitionId;
        public int partitionOffset;
        public int partitionSize;
    }

    class CgrpPartitionHeader
    {
        [FixedLength(4)]
        public string magic;
        public int partitionSize;
    }

    class CgrpPartition
    {
        public CgrpPartitionHeader header;
        public int entryCount;

        [VariableLength("entryCount")]
        public CgrpPartitionEntry[] partitionEntries;
    }

    class CgrpPartitionEntry
    {
        public int valueType;
        public int value;
    }

    class CgrpFileEntry
    {
        public int unk0;
        public int const0;
        public int dataOffset;
        public int dataSize;
    }

    class CgrpExtendedInfoEntry
    {
        public short unk1;
        public short unk2;
        public int unk3;
    }

    class CgrpArchiveFileInfo : ArchiveFileInfo
    {
        public CgrpFileEntry Entry { get; }

        public CgrpArchiveFileInfo(Stream fileData, string filePath, CgrpFileEntry entry) :
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public CgrpArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize) :
            base(fileData, filePath, configuration, decompressedSize)
        {
        }
    }

    class CgrpSupport
    {
        public static string DetermineExtension(Stream file)
        {
            using var br = new BinaryReaderX(file, true);

            switch (br.ReadString(4))
            {
                case "CWSD":
                    return ".cwsd";

                case "CWAR":
                    return ".cwar";

                case "CSEQ":
                    return ".cseq";

                default:
                    return ".bin";
            }
        }
    }
}
