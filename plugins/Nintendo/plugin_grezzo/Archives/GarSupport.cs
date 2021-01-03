using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
#pragma warning disable 649

namespace plugin_grezzo.Archives
{
    class GarHeader
    {
        [FixedLength(3)] 
        public string magic = "GAR";
        public byte version;
        public uint fileSize;

        public short fileTypeCount;
        public short fileCount;

        public int fileTypeEntryOffset;
        public int fileEntryOffset;
        public int fileOffsetsOffset;

        [FixedLength(8)]
        public string hold0; //jenkins
    }

    class Gar2FileTypeEntry
    {
        public int fileCount;
        public int fileIndexOffset;
        public int fileTypeNameOffset;
        public int unk1 = -1;
    }

    [Alignment(0x20)]
    class Gar5FileTypeEntry
    {
        public int fileCount;
        public int unk1;
        public int fileEntryIndex;
        public int fileTypeNameOffset;
        public int fileTypeInfoOffset;
    }

    class Gar5FileTypeInfo
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
