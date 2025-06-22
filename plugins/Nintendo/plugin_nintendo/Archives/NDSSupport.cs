using System.Text;
using Komponent.Contract.Aspects;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.DataClasses.FileSystem;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

/* Source: https://problemkaputt.de/gbatek.htm#dscartridgesencryptionfirmware */

namespace plugin_nintendo.Archives
{
    class NdsHeader
    {
        public string gameTitle;
        public string gameCode;
        public string makerCode;
        public UnitCode unitCode;
        public byte encryptionSeed;
        public byte deviceCapacity;
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

        public byte[] reserved3;

        public byte[] nintendoLogo;
        public short nintendoLogoCrc;

        public short headerCrc;

        public int dbgRomOffset;
        public int dbgSize;
        public int dbgLoadAddress;  // 0x168
        public int reserved4;
        public byte[] reservedDbg;
    }

    class DsiHeader
    {
        public string gameTitle;
        public string gameCode;
        public string makerCode;

        public UnitCode unitCode;
        public byte encryptionSeed;
        public byte deviceCapacity;
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

        public byte[] reserved3;

        public byte[] nintendoLogo;
        public short nintendoLogoCrc;

        public short headerCrc;

        public int dbgRomOffset;
        public int dbgSize;
        public int dbgLoadAddress;  // 0x168
        public int reserved4;
        public byte[] reservedDbg;

        public DsiExtendedEntries extendedEntries;
    }

    public struct DsiExtendedEntries
    {
        public byte[] mbkSettings;
        public byte[] arm9MbkSettings;
        public byte[] arm7MbkSettings;
        public byte[] mbk9Setting;
        public byte wramNctSettings;

        public int regionFlags;
        public int accessControl;
        public int arm7ScfgSetting;
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
        public byte[] reserved4;

        public DsiParentalControl parentalControl;

        public Sha1Section sha1Section;
    }

    public struct DsiParentalControl
    {
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
        public byte[] reserved3;
    }

    public struct Sha1Section
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

    struct FatEntry
    {
        public int offset;
        public int endOffset;

        public int Length => endOffset - offset;
    }

    struct MainFntEntry
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

    class OverlayArchiveFile : ArchiveFile
    {
        public OverlayEntry Entry { get; }

        public OverlayArchiveFile(ArchiveFileInfo fileInfo, OverlayEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }

    class FileIdArchiveFile : ArchiveFile, IFileIdArchiveFile
    {
        public int FileId { get; set; }

        public FileIdArchiveFile(ArchiveFileInfo fileInfo, int fileId) : base(fileInfo)
        {
            FileId = fileId;
        }
    }

    interface IFileIdArchiveFile : IArchiveFile
    {
        int FileId { get; set; }
    }

    static class NdsSupport
    {
        public static IEnumerable<IArchiveFile> ReadFnt(BinaryReaderX br, int fntOffset, int contentOffset, IList<FatEntry> fileEntries)
        {
            br.BaseStream.Position = fntOffset;
            var mainEntry = ReadFntEntry(br);

            br.BaseStream.Position = fntOffset;
            var mainEntries = ReadFntEntries(br, mainEntry.parentDirectory);

            foreach (var file in ReadSubFnt(br, mainEntries[0], fntOffset, contentOffset, "/", mainEntries, fileEntries))
                yield return file;
        }

        public static void WriteFnt(BinaryWriterX bw, int fntOffset, IList<IArchiveFile> files, int startFileId = 0)
        {
            var fileTree = files.ToTree();
            var totalDirectories = CountTotalDirectories(fileTree);
            var contentOffset = fntOffset + totalDirectories * 0x8;

            var baseOffset = fntOffset;
            var fileId = startFileId;
            var dirId = 0;
            WriteFnt(bw, baseOffset, ref fntOffset, ref contentOffset, ref fileId, ref dirId, 0, fileTree);

            // Write total directories
            bw.BaseStream.Position = baseOffset + 6;
            bw.Write((short)totalDirectories);
            bw.BaseStream.Position = contentOffset;
        }

        private static MainFntEntry[] ReadFntEntries(BinaryReaderX reader, int count)
        {
            var result = new MainFntEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadFntEntry(reader);

            return result;
        }

        private static MainFntEntry ReadFntEntry(BinaryReaderX reader)
        {
            return new MainFntEntry
            {
                subTableOffset = reader.ReadInt32(),
                firstFileId = reader.ReadInt16(),
                parentDirectory = reader.ReadUInt16()
            };
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
            WriteFntEntry(new MainFntEntry
            {
                subTableOffset = contentOffset - baseOffset,
                firstFileId = (short)fileId,
                parentDirectory = (ushort)(0xF000 + parentDirId)
            }, bw);
            fntOffset += 8;

            // Write file names
            bw.BaseStream.Position = contentOffset;
            foreach (var file in entry.Files.Cast<IFileIdArchiveFile>())
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

        private static void WriteFntEntry(MainFntEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.subTableOffset);
            writer.Write(entry.firstFileId);
            writer.Write(entry.parentDirectory);
        }

        private static IEnumerable<IArchiveFile> ReadSubFnt(BinaryReaderX br, MainFntEntry dirEntry, int fntOffset, int contentOffset, string path, IList<MainFntEntry> directoryEntries, IList<FatEntry> fileEntries)
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
                    yield return CreateAfi(br.BaseStream, contentOffset + currentFileEntry.offset, currentFileEntry.Length, Path.Combine(path, name), firstFileId++);
                }
                else
                {
                    // Read directory
                    var nameLength = typeLength & 0x7F;
                    var name = br.ReadString(nameLength);
                    var dirEntryId = br.ReadUInt16();
                    tableOffset = (int)br.BaseStream.Position;

                    var subDirEntry = directoryEntries[dirEntryId & 0x0FFF];
                    foreach (var file in ReadSubFnt(br, subDirEntry, fntOffset, contentOffset, Path.Combine(path, name), directoryEntries, fileEntries))
                        yield return file;
                }

                br.BaseStream.Position = tableOffset;
                typeLength = br.ReadByte();
            }
        }

        public static IArchiveFile CreateAfi(Stream input, int offset, int length, string fileName)
        {
            return new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = fileName,
                FileData = new SubStream(input, offset, length)
            });
        }

        public static IArchiveFile CreateAfi(Stream input, int offset, int length, string fileName, int fileId)
        {
            return new FileIdArchiveFile(new ArchiveFileInfo
            {
                FilePath = fileName,
                FileData = new SubStream(input, offset, length)
            }, fileId);
        }

        public static IArchiveFile CreateAfi(Stream input, int offset, int length, string fileName, OverlayEntry entry)
        {
            return new OverlayArchiveFile(new ArchiveFileInfo
            {
                FilePath = fileName,
                FileData = new SubStream(input, offset, length)
            }, entry);
        }
    }
}
