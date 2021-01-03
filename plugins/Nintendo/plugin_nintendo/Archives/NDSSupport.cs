using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.Extensions;
using Komponent.IO;
using Komponent.IO.Attributes;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
using Kontract.Models.IO;
#pragma warning disable 649

/* Source: https://problemkaputt.de/gbatek.htm#dscartridgesencryptionfirmware */

namespace plugin_nintendo.Archives
{
    class NDSHeader
    {
        [FixedLength(0xC)]
        public string gameTitle;
        [FixedLength(4)]
        public string gameCode;
        [FixedLength(2)]
        public string makerCode;

        public UnitCode unitCode;
        public byte encryptionSeed;
        public byte deviceCapacity;
        [FixedLength(7)]
        public byte[] reserved1;

        public byte reserved2;
        public byte consoleRegion;
        public byte romVer;
        public byte internalFlag;   // Bit2 enable autostart

        public int arm9Offset;
        public int arm9EntryAddress;
        public int arm9LoadAddress;
        public int arm9Size;

        public int arm7Offset;
        public int arm7EntryAddress;
        public int arm7LoadAddress;
        public int arm7Size;

        public int fntOffset;
        public int fntSize;

        public int fatOffset;
        public int fatSize;

        public int arm9OverlayOffset;
        public int arm9OverlaySize;

        public int arm7OverlayOffset;
        public int arm7OverlaySize;

        public int normalRegisterSettings;
        public int secureRegisterSettings;

        public int iconOffset;

        public short secureAreaCrc;
        public short secureTransferTimeout;

        public int arm9AutoLoad;
        public int arm7AutoLoad;

        public long secureDisable;

        public int ntrRegionSize;
        public int headerSize;

        [FixedLength(0x38)]
        public byte[] reserved3;

        [FixedLength(0x9C)]
        public byte[] nintendoLogo;
        public short nintendoLogoCrc;

        public short headerCrc;

        public int dbgRomOffset;
        public int DbgSize;
        public int DbgLoadAddress;  // 0x168
        public int reserved4;
        [FixedLength(0x90)]
        public byte[] reservedDbg;
    }

    class DSiHeader
    {
        [FixedLength(0xC)]
        public string gameTitle;
        [FixedLength(4)]
        public string gameCode;
        [FixedLength(2)]
        public string makerCode;

        public UnitCode unitCode;
        public byte encryptionSeed;
        public byte deviceCapacity;
        [FixedLength(7)]
        public byte[] reserved1;

        public byte systemFlags;
        public byte permitJump;
        public byte romVer;
        public byte internalFlag;   // Bit2 enable autostart

        public int arm9Offset;
        public int arm9EntryAddress;
        public int arm9LoadAddress;
        public int arm9Size;

        public int arm7Offset;
        public int arm7EntryAddress;
        public int arm7LoadAddress;
        public int arm7Size;

        public int fntOffset;
        public int fntSize;

        public int fatOffset;
        public int fatSize;

        public int arm9OverlayOffset;
        public int arm9OverlaySize;

        public int arm7OverlayOffset;
        public int arm7OverlaySize;

        public int normalRegisterSettings;
        public int secureRegisterSettings;

        public int iconOffset;

        public short secureAreaCrc;
        public short secureTransferTimeout;

        public int arm9AutoLoad;
        public int arm7AutoLoad;

        public long secureDisable;

        public int ntrRegionSize;
        public int headerSize;

        public int arm9ParametersOffset;
        public int arm7ParametersOffset;
        public short ntrRegionEnd;
        public short twlRegionStart;

        [FixedLength(0x2C)]
        public byte[] reserved3;

        [FixedLength(0x9C)]
        public byte[] nintendoLogo;
        public short nintendoLogoCrc;

        public short headerCrc;

        public int dbgRomOffset;
        public int DbgSize;
        public int DbgLoadAddress;  // 0x168
        public int reserved4;
        [FixedLength(0x90)]
        public byte[] reservedDbg;

        public DsiExtendedEntries extendedEntries;
    }

    public class DsiExtendedEntries
    {
        [FixedLength(0x14)]
        public byte[] mbkSettings;
        [FixedLength(0xC)]
        public byte[] arm9MbkSettings;
        [FixedLength(0xC)]
        public byte[] arm7MbkSettings;
        [FixedLength(0x3)]
        public byte[] mbk9Setting;
        public byte wramNctSettings;

        public int regionFlags;
        public int accessControl;
        public int arm7ScfgSetting;
        [FixedLength(0x3)]
        public byte[] reserved1;
        public byte flags;

        public int arm9iOffset;
        public int reserved2;
        public int arm9iLoadAddress;
        public int arm9iSize;

        public int arm7iOffset;
        public int reserved3;
        public int arm7iLoadAddress;
        public int arm7iSize;

        public int digestNtrOffset;
        public int digestNtrSize;

        public int digestTwlOffset;
        public int digestTwlSize;

        public int digestSectorHashtableOffset;
        public int digestSectorHashtableSize;

        public int digestBlockHashtableOffset;
        public int digestBlockHashtableSize;

        public int digestSectorSize;
        public int digestBlockSectorCount;

        public int iconSize;

        public byte sdmmcSize1;
        public byte sdmmcSize2;

        public byte eulaVersion;
        public bool useRatings;
        public int totalRomSize;

        public byte sdmmcSize3;
        public byte sdmmcSize4;
        public byte sdmmcSize5;
        public byte sdmmcSize6;

        public int arm9iParametersOffset;
        public int arm7iParametersOffset;

        public int modCryptArea1Offset;
        public int modCryptArea1Size;
        public int modCryptArea2Offset;
        public int modCryptArea2Size;

        public int gameCode;    // gamecode backwards
        public byte fileType;
        public byte titleIdZero0;
        public byte titleIdZeroThree;
        public byte titleIdZero1;

        public int sdmmcPublicSaveSize;
        public int sdmmcPrivateSaveSize;
        [FixedLength(0xB0)]
        public byte[] reserved4;

        public DsiParentalControl parentalControl;

        public Sha1Section sha1Section;
    }

    public class DsiParentalControl
    {
        [FixedLength(0x10)]
        public byte[] ageRatings;

        public byte cero;
        public byte esrb;
        public byte reserved1;
        public byte usk;
        public byte pegiEur;
        public byte reserved2;
        public byte pegiPrt;
        public byte bbfc;
        public byte agcb;
        public byte grb;
        [FixedLength(0x6)]
        public byte[] reserved3;
    }

    public class Sha1Section
    {
        [FixedLength(0x14)]
        public byte[] arm9HmacHash;
        [FixedLength(0x14)]
        public byte[] arm7HmacHash;
        [FixedLength(0x14)]
        public byte[] digestMasterHmacHash;
        [FixedLength(0x14)]
        public byte[] iconHmacHash;
        [FixedLength(0x14)]
        public byte[] arm9iHmacHash;
        [FixedLength(0x14)]
        public byte[] arm7iHmacHash;
        [FixedLength(0x14)]
        public byte[] reserved1;
        [FixedLength(0x14)]
        public byte[] reserved2;
        [FixedLength(0x14)]
        public byte[] arm9HmacHashWithoutSecureArea;
        [FixedLength(0xA4C)]
        public byte[] reserved3;
        [FixedLength(0x180)]
        public byte[] dbgVariableStorage;   // zero-filled in rom
        [FixedLength(0x80)]
        public byte[] headerSectionRsa;
    }

    class Arm9Footer
    {
        public uint nitroCode;
        public int unk1;
        public int unk2;
    }

    class OverlayEntry
    {
        public int id;
        public int ramAddress;
        public int ramSize;
        public int bssSize;
        public int staticInitStartAddress;
        public int staticInitEndAddress;
        public int fileId;
        public int reserved1;
    }

    class FatEntry
    {
        public int offset;
        public int endOffset;

        public int Length => endOffset - offset;
    }

    class MainFntEntry
    {
        public int subTableOffset;
        public short firstFileId;
        public ushort parentDirectory;
    }

    enum UnitCode : byte
    {
        NDS = 0,
        NDS_DSi = 2,
        DSi = 3
    }

    class OverlayArchiveFileInfo : ArchiveFileInfo
    {
        public OverlayEntry Entry { get; }

        public OverlayArchiveFileInfo(Stream fileData, string filePath, OverlayEntry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }
    }

    class FileIdArchiveFileInfo : ArchiveFileInfo, IFileIdArchiveFileInfo
    {
        public int FileId { get; set; }

        public FileIdArchiveFileInfo(Stream fileData, string filePath, int fileId) : base(fileData, filePath)
        {
            FileId = fileId;
        }
    }

    interface IFileIdArchiveFileInfo : IArchiveFileInfo
    {
        int FileId { get; set; }
    }

    static class NdsSupport
    {
        public static IEnumerable<IArchiveFileInfo> ReadFnt(BinaryReaderX br, int fntOffset, IList<FatEntry> fileEntries)
        {
            br.BaseStream.Position = fntOffset;
            var mainEntry = br.ReadType<MainFntEntry>();

            br.BaseStream.Position = fntOffset;
            var mainEntries = br.ReadMultiple<MainFntEntry>(mainEntry.parentDirectory);

            foreach (var file in ReadSubFnt(br, mainEntries[0], fntOffset, "/", mainEntries, fileEntries))
                yield return file;
        }

        public static void WriteFnt(BinaryWriterX bw, int fntOffset, IList<IArchiveFileInfo> files, int startFileId = 0)
        {
            var fileTree = files.ToTree();
            var totalDirectories = CountTotalDirectories(fileTree);
            var contentOffset = fntOffset + totalDirectories * Tools.MeasureType(typeof(MainFntEntry));

            var baseOffset = fntOffset;
            var fileId = startFileId;
            var dirId = 0;
            WriteFnt(bw, baseOffset, ref fntOffset, ref contentOffset, ref fileId, ref dirId, 0, fileTree);

            // Write total directories
            bw.BaseStream.Position = baseOffset + 6;
            bw.Write((short)totalDirectories);
            bw.BaseStream.Position = contentOffset;
        }

        private static int CountTotalDirectories(DirectoryEntry dirEntry)
        {
            var result = 1;
            foreach (var entry in dirEntry.Directories)
                result += CountTotalDirectories(entry);

            return result;
        }

        private static void WriteFnt(BinaryWriterX bw, int baseOffset, ref int fntOffset, ref int contentOffset, ref int fileId, ref int dirId, int parentDirId, DirectoryEntry entry)
        {
            // Write dir entry
            bw.BaseStream.Position = fntOffset;
            bw.WriteType(new MainFntEntry
            {
                subTableOffset = contentOffset - baseOffset,
                firstFileId = (short)fileId,
                parentDirectory = (ushort)(0xF000 + parentDirId)
            });
            fntOffset += 8;

            // Write file names
            bw.BaseStream.Position = contentOffset;
            foreach (var file in entry.Files.Cast<IFileIdArchiveFileInfo>())
            {
                bw.WriteString(file.FilePath.GetName(), Encoding.ASCII, true, false);
                file.FileId = fileId++;
            }
            contentOffset = (int)bw.BaseStream.Position;

            // Write directory entries
            var nextContentOffset = (int)(bw.BaseStream.Position + entry.Directories.Sum(x => x.Name.Length + 3) + 1);
            var currentDirId = dirId;
            foreach (var dir in entry.Directories)
            {
                bw.BaseStream.Position = contentOffset;

                bw.Write((byte)(dir.Name.Length + 0x80));
                bw.WriteString(dir.Name, Encoding.ASCII, false, false);
                bw.Write((ushort)(0xF000 + ++dirId));

                contentOffset = (int)bw.BaseStream.Position;

                WriteFnt(bw, baseOffset, ref fntOffset, ref nextContentOffset, ref fileId, ref dirId, currentDirId, dir);
            }

            contentOffset = nextContentOffset;
        }

        private static IEnumerable<IArchiveFileInfo> ReadSubFnt(BinaryReaderX br, MainFntEntry dirEntry, int fntOffset, string path, IList<MainFntEntry> directoryEntries, IList<FatEntry> fileEntries)
        {
            var tableOffset = fntOffset + dirEntry.subTableOffset;
            var firstFileId = dirEntry.firstFileId;

            br.BaseStream.Position = tableOffset;

            var typeLength = br.ReadByte();
            while (typeLength != 0)
            {
                if (typeLength == 0x80)
                    throw new InvalidOperationException("TypeLength 0x80 is reserved.");

                if (typeLength <= 0x7F)
                {
                    // Read file
                    var name = br.ReadString(typeLength);
                    tableOffset = (int)br.BaseStream.Position;

                    var currentFileEntry = fileEntries[firstFileId];
                    yield return CreateAfi(br.BaseStream, currentFileEntry.offset, currentFileEntry.Length, Path.Combine(path, name), firstFileId++);
                }
                else
                {
                    // Read directory
                    var nameLength = typeLength & 0x7F;
                    var name = br.ReadString(nameLength);
                    var dirEntryId = br.ReadUInt16();
                    tableOffset = (int)br.BaseStream.Position;

                    var subDirEntry = directoryEntries[dirEntryId & 0x0FFF];
                    foreach (var file in ReadSubFnt(br, subDirEntry, fntOffset, Path.Combine(path, name), directoryEntries, fileEntries))
                        yield return file;
                }

                br.BaseStream.Position = tableOffset;
                typeLength = br.ReadByte();
            }
        }

        public static IArchiveFileInfo CreateAfi(Stream input, int offset, int length, string fileName)
        {
            return new ArchiveFileInfo(new SubStream(input, offset, length), fileName);
        }

        public static IArchiveFileInfo CreateAfi(Stream input, int offset, int length, string fileName, int fileId)
        {
            return new FileIdArchiveFileInfo(new SubStream(input, offset, length), fileName, fileId);
        }

        public static IArchiveFileInfo CreateAfi(Stream input, int offset, int length, string fileName, OverlayEntry entry)
        {
            return new OverlayArchiveFileInfo(new SubStream(input, offset, length), fileName, entry);
        }
    }
}
