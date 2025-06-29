using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.DataClasses.FileSystem;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_criware.Archives
{
    class Cvm
    {
        private const int EntrySize_ = 0x21;

        private static readonly IList<string> Passwords = new List<string>
        {
            "zxcv",
            "cc2fuku",
            "shinobutan",
            "MELTYBLOOD_AA",
            "PJ234110",
            "4147a5c2b5fe0357",
            "tinaandluckandru",
            "SAGUCHIFUNAYOI",
            "itinenmotanai",
            "qi2o@9a!"
        };

        private CvmHeader _header;
        private CvmZoneInfo _zoneInfo;
        private byte[] _unkDataLoc;
        private IsoPrimaryDescriptor _primeDesc;
        private string _detectedPassword;

        #region Load

        public List<IArchiveFile> Load(Stream input)
        {
            // Prepare binary readers
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            var isoStream = new SubStream(input, 0x1800, input.Length - 0x1800);

            // Read header
            _header = ReadHeader(br);
            br.SeekAlignment(0x800);

            _zoneInfo = ReadZoneInfo(br);
            br.SeekAlignment(0x800);

            _unkDataLoc = br.ReadBytes(0x800);

            // Prepare decryption stream
            Stream decStream = isoStream;
            if (_header.IsEncrypted)
            {
                _detectedPassword = DetectPassword(isoStream);
                decStream = new RofsCryptoStream(isoStream, _detectedPassword, 0, 0x800);
            }

            using var decBr = new BinaryReaderX(decStream);

            // Read ISO primary descriptor
            decStream.Position = 0x8000;
            _primeDesc = ReadPrimaryDescriptor(decBr);
            decBr.SeekAlignment(0x800);

            // Read record tree
            return ParseDirTree(decBr, isoStream, _primeDesc.rootDirRecord.extentLe, _primeDesc.rootDirRecord.sizeLe, UPath.Root).ToList();
        }

        private IEnumerable<IArchiveFile> ParseDirTree(BinaryReaderX br, Stream isoStream, uint dirExtent, uint dirSize, UPath path)
        {
            long currentPosition = dirExtent * 0x800;
            while (dirSize > 0)
            {
                var dirChunk = Math.Min(0x800, dirSize);
                dirSize -= dirChunk;

                while (dirChunk > 0)
                {
                    br.BaseStream.Position = currentPosition;

                    // Check if alignment to next sector is needed
                    var length = br.ReadByte();
                    if (length == 0)
                    {
                        br.SeekAlignment(0x800);
                        currentPosition = br.BaseStream.Position;
                        break;
                    }

                    // Read dir entry
                    br.BaseStream.Position--;
                    var dirRecord = ReadDirectoryRecord(br);
                    currentPosition = br.BaseStream.Position;

                    if ((dirRecord.flags & 2) > 0)
                    {
                        if (dirRecord.nameLength != 1 || dirRecord.name[0] != '\0' && dirRecord.name[0] != 1)
                            foreach (var file in ParseDirTree(br, isoStream, dirRecord.extentLe, dirRecord.sizeLe, path / dirRecord.name))
                                yield return file;
                    }
                    else
                    {
                        yield return new ArchiveFile(new ArchiveFileInfo
                        {
                            FilePath = (path / dirRecord.name.Split(';')[0]).FullName,
                            FileData = new SubStream(isoStream, dirRecord.extentLe * 0x800, dirRecord.sizeLe)
                        });
                    }

                    dirChunk -= dirRecord.length;
                }
            }
        }

        private CvmHeader ReadHeader(BinaryReaderX reader)
        {
            var header = new CvmHeader
            {
                magic = reader.ReadString(4),
                headerSize = reader.ReadInt64(),
                reserved1 = reader.ReadBytes(0x10),
                fileSize = reader.ReadInt64(),
                date = reader.ReadBytes(7),
                padding1 = reader.ReadByte(),
                version1 = reader.ReadInt32(),
                flags = reader.ReadInt32(),
                rofsMagic = reader.ReadString(4),
                makeToolId = reader.ReadString(0x40),
                version2 = reader.ReadInt32(),
                unk1 = reader.ReadByte(),
                unk2 = reader.ReadByte(),
                unk3 = reader.ReadInt16(),
                sectorCount = reader.ReadInt32(),
                zoneSector = reader.ReadInt32(),
                isoSectorStart = reader.ReadInt32(),
                padding = reader.ReadBytes(0x74)
            };

            header.sectorCounts = ReadIntegers(reader, header.sectorCount);

            return header;
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private CvmZoneInfo ReadZoneInfo(BinaryReaderX reader)
        {
            return new CvmZoneInfo
            {
                magic = reader.ReadString(4),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadInt32(),
                unk4 = reader.ReadBytes(8),
                sectorLength1 = reader.ReadInt32(),
                sectorLength2 = reader.ReadInt32(),
                dataLoc1 = ReadZoneLoc(reader),
                isoDataLoc = ReadZoneLoc(reader)
            };
        }

        private CvmZoneDataLoc ReadZoneLoc(BinaryReaderX reader)
        {
            return new CvmZoneDataLoc
            {
                sectorIndex = reader.ReadInt32(),
                length = reader.ReadInt64()
            };
        }

        private IsoPrimaryDescriptor ReadPrimaryDescriptor(BinaryReaderX reader)
        {
            ByteOrder byteOrder = reader.ByteOrder;

            var descriptor = new IsoPrimaryDescriptor
            {
                type = reader.ReadByte(),
                id = reader.ReadString(5),
                version = reader.ReadByte(),
                unused1 = reader.ReadByte(),
                system_id = reader.ReadString(0x20),
                volume_id = reader.ReadString(0x20),
                unused2 = reader.ReadBytes(8)
            };

            reader.ByteOrder = ByteOrder.LittleEndian;
            descriptor.volSizeLe = reader.ReadInt32();

            reader.ByteOrder = ByteOrder.BigEndian;
            descriptor.volSizeBe = reader.ReadInt32();

            reader.ByteOrder = byteOrder;
            descriptor.escapeSequences = reader.ReadBytes(0x20);
            descriptor.volSetSize = reader.ReadInt32();
            descriptor.volSequenceNumber = reader.ReadInt32();

            reader.ByteOrder = ByteOrder.LittleEndian;
            descriptor.logicalBlockSizeLe = reader.ReadInt16();

            reader.ByteOrder = ByteOrder.BigEndian;
            descriptor.logicalBlockSizeBe = reader.ReadInt16();

            reader.ByteOrder = ByteOrder.LittleEndian;
            descriptor.pathTableSizeLe = reader.ReadInt32();

            reader.ByteOrder = ByteOrder.BigEndian;
            descriptor.pathTableSizeBe = reader.ReadInt32();

            reader.ByteOrder = byteOrder;
            descriptor.typelPathTable = reader.ReadInt32();
            descriptor.optTypelPathTable = reader.ReadInt32();
            descriptor.typemPathTable = reader.ReadInt32();
            descriptor.optTypemPathTable = reader.ReadInt32();

            descriptor.rootDirRecord = ReadDirectoryRecord(reader);
            reader.SeekAlignment(2);

            descriptor.volumeSetId = reader.ReadString(0x80);
            descriptor.publisherId = reader.ReadString(0x80);
            descriptor.preparerId = reader.ReadString(0x80);
            descriptor.applicationId = reader.ReadString(0x80);
            descriptor.copyrightFileId = reader.ReadString(0x25);
            descriptor.abstractFileId = reader.ReadString(0x25);
            descriptor.bibliographicFileId = reader.ReadString(0x25);
            descriptor.creationDate = reader.ReadString(0x11);
            descriptor.modificationDate = reader.ReadString(0x11);
            descriptor.expirationDate = reader.ReadString(0x11);
            descriptor.effectiveDate = reader.ReadString(0x11);
            descriptor.fileStructureVersion = reader.ReadByte();
            descriptor.unused4 = reader.ReadByte();
            descriptor.applicationData = reader.ReadBytes(0x200);

            return descriptor;
        }

        private IsoDirectoryRecord ReadDirectoryRecord(BinaryReaderX reader)
        {
            ByteOrder byteOrder = reader.ByteOrder;

            var record = new IsoDirectoryRecord
            {
                length = reader.ReadByte(),
                extAttributeLength = reader.ReadByte(),
            };

            reader.ByteOrder = ByteOrder.LittleEndian;
            record.extentLe = reader.ReadUInt32();

            reader.ByteOrder = ByteOrder.BigEndian;
            record.extentBe = reader.ReadUInt32();

            reader.ByteOrder = ByteOrder.LittleEndian;
            record.sizeLe = reader.ReadUInt32();

            reader.ByteOrder = ByteOrder.BigEndian;
            record.sizeBe = reader.ReadUInt32();

            reader.ByteOrder = byteOrder;
            record.date = reader.ReadBytes(7);
            record.flags = reader.ReadByte();
            record.fileUnitSize = reader.ReadByte();
            record.interleave = reader.ReadByte();
            record.volumeSequenceNumber = reader.ReadInt32();

            record.nameLength = reader.ReadByte();
            record.name = reader.ReadString(record.nameLength);

            return record;
        }

        #endregion

        #region Save

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            var fileTree = files.ToTree();

            // Pre-calculate size of the TOC
            var dirTotalSize = CalculateDirTreeSize(fileTree);
            var dirOnlySize = CalculateDirTreeSize(fileTree, false);

            // Prepare streams
            output.SetLength(0xB800 + dirTotalSize);
            Stream tocStream = new SubStream(output, 0x1800, 0xA000 + dirTotalSize);
            if (_header.IsEncrypted)
                tocStream = new RofsCryptoStream(tocStream, _detectedPassword, 0, 0x800);

            // Write files and TOC
            using var tocBw = new BinaryWriterX(tocStream);
            var fileOffset = (long)(0xB800 + dirTotalSize);
            WriteDirTree(fileTree, output, tocBw, 0xA000, ref fileOffset);

            // Write pre TOC information
            _primeDesc.volSizeBe = (int)((output.Length - 0x1800 + 0x7FF) & ~0x7FF);
            _primeDesc.volSizeLe = (int)((output.Length - 0x1800 + 0x7FF) & ~0x7FF);
            _primeDesc.logicalBlockSizeLe = 0x800;
            _primeDesc.logicalBlockSizeBe = 0x800;
            _primeDesc.rootDirRecord.sizeLe = (uint)dirOnlySize;
            _primeDesc.rootDirRecord.sizeBe = (uint)dirOnlySize;

            tocStream.Position = 0x8000;
            WritePrimaryDescriptor(_primeDesc, tocBw);
            tocBw.WriteAlignment(0x800);

            // Write end ISO sector
            tocBw.Write((byte)0xFF);
            tocBw.WriteString("CD001", Encoding.ASCII, false, false);
            tocBw.Write((short)0x1);
            tocBw.WriteAlignment(0x800);

            // Write root dir information in little endian
            tocBw.Write((short)0x1);
            tocBw.Write(0x14);
            tocBw.Write((short)1);
            tocBw.WriteAlignment(0x800);

            // Write root dir information in big endian
            tocBw.Write((short)0x1);
            tocBw.ByteOrder = ByteOrder.BigEndian;
            tocBw.Write(0x14);
            tocBw.Write((short)1);

            // Write CVM header information
            _header.fileSize = output.Length;
            _zoneInfo.isoDataLoc.length = output.Length - 0x1800;

            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);
            output.Position = 0;

            WriteHeader(_header, bw);
            bw.WriteAlignment(0x800);

            WriteZoneInfo(_zoneInfo, bw);
            bw.WriteAlignment(0x800);

            bw.Write(_unkDataLoc);
        }

        private void WriteHeader(CvmHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.headerSize);
            writer.Write(header.reserved1);
            writer.Write(header.fileSize);
            writer.Write(header.date);
            writer.Write(header.padding1);
            writer.Write(header.version1);
            writer.Write(header.flags);
            writer.WriteString(header.rofsMagic, writeNullTerminator: false);
            writer.WriteString(header.makeToolId, writeNullTerminator: false);
            writer.Write(header.version2);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
            writer.Write(header.sectorCount);
            writer.Write(header.zoneSector);
            writer.Write(header.isoSectorStart);
            writer.Write(header.padding);

            WriteIntegers(header.sectorCounts, writer);
        }

        private void WriteIntegers(int[] entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }

        private void WriteZoneInfo(CvmZoneInfo zoneInfo, BinaryWriterX writer)
        {
            writer.WriteString(zoneInfo.magic, writeNullTerminator: false);
            writer.Write(zoneInfo.unk1);
            writer.Write(zoneInfo.unk2);
            writer.Write(zoneInfo.unk3);
            writer.Write(zoneInfo.unk4);
            writer.Write(zoneInfo.sectorLength1);
            writer.Write(zoneInfo.sectorLength2);

            WriteZoneLoc(zoneInfo.dataLoc1, writer);
            WriteZoneLoc(zoneInfo.isoDataLoc, writer);
        }

        private void WriteZoneLoc(CvmZoneDataLoc zoneLoc, BinaryWriterX writer)
        {
            writer.Write(zoneLoc.sectorIndex);
            writer.Write(zoneLoc.length);
        }

        private void WritePrimaryDescriptor(IsoPrimaryDescriptor primaryDescriptor, BinaryWriterX writer)
        {
            ByteOrder byteOrder = writer.ByteOrder;

            writer.Write(primaryDescriptor.type);
            writer.WriteString(primaryDescriptor.id, writeNullTerminator: false);
            writer.Write(primaryDescriptor.version);
            writer.Write(primaryDescriptor.unused1);
            writer.Write(primaryDescriptor.system_id);
            writer.Write(primaryDescriptor.volume_id);
            writer.Write(primaryDescriptor.unused2);

            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.Write(primaryDescriptor.volSizeLe);

            writer.ByteOrder = ByteOrder.BigEndian;
            writer.Write(primaryDescriptor.volSizeBe);

            writer.ByteOrder = byteOrder;
            writer.Write(primaryDescriptor.escapeSequences);
            writer.Write(primaryDescriptor.volSetSize);
            writer.Write(primaryDescriptor.volSequenceNumber);

            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.Write(primaryDescriptor.logicalBlockSizeLe);

            writer.ByteOrder = ByteOrder.BigEndian;
            writer.Write(primaryDescriptor.logicalBlockSizeBe);

            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.Write(primaryDescriptor.pathTableSizeLe);

            writer.ByteOrder = ByteOrder.BigEndian;
            writer.Write(primaryDescriptor.pathTableSizeBe);

            writer.ByteOrder = byteOrder;
            writer.Write(primaryDescriptor.typelPathTable);
            writer.Write(primaryDescriptor.optTypelPathTable);
            writer.Write(primaryDescriptor.typemPathTable);
            writer.Write(primaryDescriptor.optTypemPathTable);

            WriteDirectoryRecord(primaryDescriptor.rootDirRecord, writer);
            writer.WriteAlignment(2);

            writer.WriteString(primaryDescriptor.volumeSetId, writeNullTerminator: false);
            writer.WriteString(primaryDescriptor.publisherId, writeNullTerminator: false);
            writer.WriteString(primaryDescriptor.preparerId, writeNullTerminator: false);
            writer.WriteString(primaryDescriptor.applicationId, writeNullTerminator: false);
            writer.WriteString(primaryDescriptor.copyrightFileId, writeNullTerminator: false);
            writer.WriteString(primaryDescriptor.abstractFileId, writeNullTerminator: false);
            writer.WriteString(primaryDescriptor.bibliographicFileId, writeNullTerminator: false);
            writer.WriteString(primaryDescriptor.creationDate, writeNullTerminator: false);
            writer.WriteString(primaryDescriptor.modificationDate, writeNullTerminator: false);
            writer.WriteString(primaryDescriptor.expirationDate, writeNullTerminator: false);
            writer.WriteString(primaryDescriptor.effectiveDate, writeNullTerminator: false);
            writer.Write(primaryDescriptor.fileStructureVersion);
            writer.Write(primaryDescriptor.unused4);
            writer.Write(primaryDescriptor.applicationData);
        }

        private void WriteDirectoryRecord(IsoDirectoryRecord record, BinaryWriterX writer)
        {
            ByteOrder byteOrder = writer.ByteOrder;

            writer.Write(record.length);
            writer.Write(record.extAttributeLength);

            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.Write(record.extentLe);

            writer.ByteOrder = ByteOrder.BigEndian;
            writer.Write(record.extentBe);

            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.Write(record.sizeLe);

            writer.ByteOrder = ByteOrder.BigEndian;
            writer.Write(record.sizeBe);

            writer.ByteOrder = byteOrder;
            writer.Write(record.date);
            writer.Write(record.flags);
            writer.Write(record.fileUnitSize);
            writer.Write(record.interleave);
            writer.Write(record.volumeSequenceNumber);

            writer.Write(record.nameLength);
            writer.WriteString(record.name, writeNullTerminator: false);
        }

        private int CalculateDirTreeSize(DirectoryEntry dirEntry, bool calculateSubDirs = true, bool first = true)
        {
            var totalSize = 0;
            var sectorFilled = first ? (EntrySize_ + dirEntry.Name.Length + 1) & ~1 : 0;

            // First calculate all file entries of the directory
            foreach (var file in dirEntry.Files)
            {
                var entrySize = (EntrySize_ + file.FilePath.ToRelative().FullName.Length + 2 + 1) & ~1;
                if (sectorFilled + entrySize >= 0x800)
                {
                    totalSize += 0x800;
                    sectorFilled = 0;
                }

                sectorFilled += entrySize;
            }

            // Then calculate all directory entries
            foreach (var dir in dirEntry.Directories)
            {
                var entrySize = (EntrySize_ + dir.Name.Length + 1) & ~1;
                if (sectorFilled + entrySize >= 0x800)
                {
                    totalSize += 0x800;
                    sectorFilled = 0;
                }

                sectorFilled += entrySize;

                if (calculateSubDirs)
                    totalSize += CalculateDirTreeSize(dir, true, false);
            }

            if (sectorFilled != 0)
                totalSize += 0x800;

            return totalSize;
        }

        private void WriteDirTree(DirectoryEntry dirEntry, Stream input, BinaryWriterX tocBw, long entryOffset, ref long fileOffset, bool first = true)
        {
            var totalSize = 0;
            var sectorFilled = first ? (EntrySize_ + dirEntry.Name.Length + 1) & ~1 : 0;

            // Write file entries
            tocBw.BaseStream.Position = entryOffset + totalSize + sectorFilled;
            foreach (var file in dirEntry.Files)
            {
                var entrySize = (EntrySize_ + file.FilePath.GetName().Length + 3) & ~1;

                // Write file
                input.Position = fileOffset;
                file.WriteFileData(input);
                while (input.Position % 0x800 != 0)
                    input.WriteByte(0);

                // Advance positioning
                if (sectorFilled + entrySize >= 0x800)
                {
                    totalSize += 0x800;
                    sectorFilled = 0;
                    tocBw.WriteAlignment(0x800);
                }

                // Write file entry
                var record = new IsoDirectoryRecord
                {
                    length = (byte)entrySize,
                    extentBe = (uint)((fileOffset - 0x1800) / 0x800),
                    extentLe = (uint)((fileOffset - 0x1800) / 0x800),
                    sizeBe = (uint)file.FileSize,
                    sizeLe = (uint)file.FileSize,
                    date = new byte[7],
                    flags = 0,
                    volumeSequenceNumber = 0x10000001,
                    nameLength = (byte)(file.FilePath.GetName().Length + 2),
                    name = file.FilePath.GetName() + ";1"
                };

                tocBw.BaseStream.Position = entryOffset + totalSize + sectorFilled;
                WriteDirectoryRecord(record, tocBw);

                sectorFilled += entrySize;
                fileOffset += (file.FileSize + 0x7FF) & ~0x7FF;
            }

            // Calculate first sub dir offset
            var subDirOffset = totalSize;
            var sectorFilled2 = sectorFilled;
            foreach (var dir in dirEntry.Directories)
            {
                var entrySize = (EntrySize_ + dir.Name.Length + 1) & ~1;
                if (sectorFilled2 + entrySize >= 0x800)
                {
                    subDirOffset += 0x800;
                    sectorFilled2 = 0;
                }

                sectorFilled2 += entrySize;
            }

            if (sectorFilled2 != 0)
                subDirOffset += 0x800;

            // Write directory entries
            foreach (var dir in dirEntry.Directories)
            {
                var dirSize = CalculateDirTreeSize(dir, false);
                var entrySize = (EntrySize_ + dir.Name.Length + 1) & ~1;

                // Write sub directory
                WriteDirTree(dir, input, tocBw, entryOffset + subDirOffset, ref fileOffset, false);

                // Advance positioning
                if (sectorFilled + entrySize >= 0x800)
                {
                    totalSize += 0x800;
                    sectorFilled = 0;
                    tocBw.WriteAlignment(0x800);
                }

                // Write sub directory entry
                var record = new IsoDirectoryRecord
                {
                    length = (byte)entrySize,
                    extentBe = (uint)((entryOffset + subDirOffset) / 0x800),
                    extentLe = (uint)((entryOffset + subDirOffset) / 0x800),
                    sizeBe = (uint)dirSize,
                    sizeLe = (uint)dirSize,
                    date = new byte[7],
                    flags = 2,
                    volumeSequenceNumber = 0x10000001,
                    nameLength = (byte)dir.Name.Length,
                    name = dir.Name
                };

                tocBw.BaseStream.Position = entryOffset + totalSize + sectorFilled;
                WriteDirectoryRecord(record, tocBw);

                sectorFilled += entrySize;
                subDirOffset += dirSize;
            }

            if (sectorFilled != 0)
            {
                totalSize += 0x800;
                tocBw.WriteAlignment(0x800);
            }

            // Write current directory entry
            if (first)
            {
                var bkPos = tocBw.BaseStream.Position;

                var record = new IsoDirectoryRecord
                {
                    length = (byte)((EntrySize_ + dirEntry.Name.Length + 1) & ~1),
                    extentBe = (uint)(entryOffset / 0x800),
                    extentLe = (uint)(entryOffset / 0x800),
                    sizeBe = (uint)totalSize,
                    sizeLe = (uint)totalSize,
                    date = new byte[7],
                    flags = 2,
                    volumeSequenceNumber = 0x10000001,
                    nameLength = (byte)(dirEntry.Name.Length == 0 ? 1 : dirEntry.Name.Length),
                    name = string.IsNullOrEmpty(dirEntry.Name) ? "\0" : dirEntry.Name
                };

                tocBw.BaseStream.Position = entryOffset;
                WriteDirectoryRecord(record, tocBw);

                tocBw.BaseStream.Position = bkPos;
            }
        }

        #endregion

        private string DetectPassword(Stream input)
        {
            input = new SubStream(input, 0x8000, 0x800);

            foreach (var pw in Passwords)
            {
                using var cipher = new RofsCryptoStream(input, pw, 0x10, 0x800);
                using var cipherBr = new BinaryReaderX(cipher);

                cipher.Position = 1;
                if (cipherBr.ReadString(5) == "CD001")
                    return pw;
            }

            throw new InvalidOperationException("Password could not be detected. Please report this on the github of the developers of Kuriimu2.");
        }
    }
}
