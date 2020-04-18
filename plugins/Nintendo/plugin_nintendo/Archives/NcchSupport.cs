using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Komponent.IO;
using Komponent.IO.Attributes;

namespace plugin_nintendo.Archives
{

    class NcchHeader
    {
        [FixedLength(0x100)]
        public byte[] rsa2048;
        [FixedLength(4)]
        public string magic;
        public int ncchSize;
        public ulong partitionId;
        public short makerCode;
        public short version;
        public uint seedHashVerifier;
        public ulong programID;
        [FixedLength(0x10)]
        public byte[] reserved1;
        [FixedLength(0x20)]
        public byte[] logoRegionHash;
        [FixedLength(0x10)]
        public byte[] productCode;
        [FixedLength(0x20)]
        public byte[] exHeaderHash;
        public int exHeaderSize;
        public int reserved2;
        [FixedLength(0x8)]
        public byte[] ncchFlags;
        public int plainRegionOffset;
        public int plainRegionSize;
        public int logoRegionOffset;
        public int logoRegionSize;
        public int exeFsOffset;
        public int exeFsSize;
        public int exeFSHashRegSize;
        public int reserved3;
        public int romFSOffset;
        public int romFSSize;
        public int romFSHashRegSize;
        public int reserved4;
        [FixedLength(0x20)]
        public byte[] exeFSSuperBlockHash;
        [FixedLength(0x20)]
        public byte[] romFSSuperBlockHash;
    }

    class NcchExeFsHeader
    {
        [FixedLength(0xA)]
        public List<NcchExeFsFileEntry> fileHeaders;
        [FixedLength(0x20)]
        public byte[] reserved1;
        [FixedLength(0xA)]
        public List<NcchExeFsFileEntryHash> fileHeaderHash;
    }

    class NcchExeFsFileEntry
    {
        [FixedLength(8)]
        public string name;
        public int offset;
        public int size;
    }

    class NcchExeFsFileEntryHash
    {
        [FixedLength(0x20)]
        public byte[] hash;
    }

    class NcchRomFs
    {
        NcchRomFsLevelHeader lv3Header;
        long lv3Offset;

        public NcchRomFsHeader header;
        public byte[] masterHash;

        public List<FinalFileInfo> Files { get; }

        public NcchRomFs(Stream instream)
        {
            using var br = new BinaryReaderX(instream, true);

            // Read header
            header = br.ReadType<NcchRomFsHeader>();
            masterHash = br.ReadBytes(header.masterHashSize);

            // Read Level 3
            br.SeekAlignment(1 << header.lv3BlockSize);
            lv3Offset = br.BaseStream.Position;
            lv3Header = br.ReadType<NcchRomFsLevelHeader>();

            // Resolve file and directory tree
            br.BaseStream.Position = lv3Offset + lv3Header.dirMetaTableOffset;
            Files = new List<FinalFileInfo>();
            ResolveDirectories(br);
        }

        private void ResolveDirectories(BinaryReaderX br, string currentPath = "")
        {
            var currentDirEntry = br.ReadType<NcchRomFsDirectoryMetaData>();

            // First go through all sub dirs
            if (currentDirEntry.firstChildDirOffset != -1)
            {
                br.BaseStream.Position = lv3Offset + lv3Header.dirMetaTableOffset + currentDirEntry.firstChildDirOffset;
                ResolveDirectories(br, currentPath + currentDirEntry.name + "/");
            }

            // Then get all Files of current directory
            if (currentDirEntry.firstFileOffset != -1)
            {
                var fileOffset = currentDirEntry.firstFileOffset;

                // Move through sibling Files without recursion
                NcchRomFsFileMetaData currentFileEntry;
                do
                {
                    br.BaseStream.Position = lv3Offset + lv3Header.fileMetaTableOffset + fileOffset;
                    currentFileEntry = br.ReadType<NcchRomFsFileMetaData>();

                    // Add current file
                    Files.Add(new FinalFileInfo
                    {
                        filePath = currentPath + currentDirEntry.name + "/" + currentFileEntry.name,
                        fileOffset = lv3Offset + lv3Header.fileDataOffset + currentFileEntry.fileOffset,
                        fileSize = currentFileEntry.fileSize
                    });

                    fileOffset = currentFileEntry.nextSiblingFileOffset;
                } while (currentFileEntry.nextSiblingFileOffset != -1);
            }

            // Move to next sibling directory
            if (currentDirEntry.nextSiblingDirOffset != -1)
            {
                br.BaseStream.Position = lv3Offset + lv3Header.dirMetaTableOffset + currentDirEntry.nextSiblingDirOffset;
                ResolveDirectories(br, currentPath);
            }
        }

        [DebuggerDisplay("{filePath}")]
        public class FinalFileInfo
        {
            public string filePath;
            public long fileOffset;
            public long fileSize;
        }
    }

    [Alignment(0x10)]
    class NcchRomFsHeader
    {
        [FixedLength(4)]
        public string magic = "IVFC";
        public int magicNumber = 0x10000;
        public int masterHashSize;
        public long lv1LogicalOffset;
        public long lv1HashDataSize;
        public int lv1BlockSize = 0xC;
        public int reserved1 = 0;
        public long lv2LogicalOffset;
        public long lv2HashDataSize;
        public int lv2BlockSize = 0xC;
        public int reserved2 = 0;
        public long lv3LogicalOffset;
        public long lv3HashDataSize;
        public int lv3BlockSize = 0xC;
        public int reserved3 = 0;
        public int headerLength = 0x5C;
        public int infoSize = 0;
    }

    class NcchRomFsLevelHeader
    {
        public int headerLength;
        public int dirHashTableOffset;
        public int dirHashTableSize;
        public int dirMetaTableOffset;
        public int dirMetaTableSize;
        public int fileHashTableOffset;
        public int fileHashTableSize;
        public int fileMetaTableOffset;
        public int fileMetaTableSize;
        public int fileDataOffset;
    }

    class NcchRomFsDirectoryMetaData
    {
        public int parentDirOffset;
        public int nextSiblingDirOffset;
        public int firstChildDirOffset;
        public int firstFileOffset;
        public int nextDirInSameBucketOffset;
        public int nameLength;
        [VariableLength("nameLength", StringEncoding = StringEncoding.Unicode)]
        public string name;
    }

    class NcchRomFsFileMetaData
    {
        public int containingDirOffset;
        public int nextSiblingFileOffset;
        public long fileOffset;
        public long fileSize;
        public int nextFileInSameBucketOffset;
        public int nameLength;
        [VariableLength("nameLength", StringEncoding = StringEncoding.Unicode)]
        public string name;
    }
}
