using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_grezzo.Archives
{
    class GarHeader
    {
        [FixedLength(3)] 
        public string magic = "GAR";
        public byte version;
        public uint fileSize;

        public short directoryCount;
        public short fileCount;

        public int directoryEntryOffset;
        public int fileEntryOffset;
        public int filePositionOffset;

        [FixedLength(8)]
        public string hold0; //jenkins
    }

    class Gar2DirectoryEntry
    {
        public int fileCount;
        public int fileIdOffset;
        public int directoryNameOffset;
        public int unk1 = -1;
    }

    [Alignment(0x20)]
    class Gar5DirectoryEntry
    {
        public int fileCount;
        public int unk1;
        public int fileEntryIndex;
        public int directoryNameOffset;
        public int directoryInfoOffset;
    }

    class Gar5DirectoryInfo
    {
        public int unk1;
        public int unk2;
        public short unk3;
        public short unk4;
    }

    class Gar2FileEntry
    {
        public uint fileSize;
        public int nameOffset;
        public int fileNameOffset;
    }

    class Gar5FileEntry
    {
        public int fileSize;
        public int fileOffset;
        public int fileNameOffset;
        public int unk1 = -1;
    }
}
