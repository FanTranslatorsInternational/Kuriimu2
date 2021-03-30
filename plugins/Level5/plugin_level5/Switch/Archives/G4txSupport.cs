using System.Collections.Generic;
using System.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_level5.Switch.Archives
{
    class G4txHeader
    {
        [FixedLength(4)]
        public string magic = "G4TX";
        public short headerSize = 0x60;
        public short fileType = 0x65;
        public int unk1 = 0x00180000;
        public int tableSize;
        [FixedLength(0x10)]
        public byte[] zeroes;
        public short textureCount;
        public short totalCount;
        public byte unk2;
        public byte subTextureCount;
        public short unk3;
        public int unk4;
        public int textureDataSize;
        public long unk5;
        [FixedLength(0x28)]
        public byte[] unk6;
    }

    class G4txEntry
    {
        public int unk1;
        public int nxtchOffset;
        public int nxtchSize;
        public int unk2;
        public int unk3;
        public int unk4;
        public short width;
        public short height;
        public int const2 = 1;
        [FixedLength(0x10)]
        public byte[] unk5;
    }

    class G4txSubEntry
    {
        public short entryId;
        public short unk1;
        public short x;
        public short y;
        public short width;
        public short height;
        public int unk2;
        public int unk3;
        public int unk4;
    }

    class G4txSubTextureEntry
    {
        public byte Id { get; }

        public G4txSubEntry EntryEntry { get; }

        public string Name { get; }

        public G4txSubTextureEntry(byte id, G4txSubEntry entryEntry,string name)
        {
            Id = id;
            EntryEntry = entryEntry;
            Name = name;
        }
    }

    class G4txArchiveFileInfo : ArchiveFileInfo
    {
        public G4txEntry Entry { get; }

        public byte Id { get; }

        public IList<G4txSubTextureEntry> Entries { get; }

        public G4txArchiveFileInfo(Stream fileData, string filePath, G4txEntry entry, byte id, IList<G4txSubTextureEntry> entries) : base(fileData, filePath)
        {
            Entry = entry;
            Id = id;
            Entries = entries;
        }
    }
}
