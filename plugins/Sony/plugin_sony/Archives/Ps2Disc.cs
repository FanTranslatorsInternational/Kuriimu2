using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_sony.Archives
{
    class Ps2Disc
    {
        private const int SectorSize_ = 0x800;
        private const int DescriptorStart_ = 0x10 * SectorSize_;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read volume descriptor
            input.Position = DescriptorStart_;
            var descriptor = ReadVolumeDescriptor(br);

            // Read directory entries
            return ReadDirectories(br, descriptor.descriptorPrimary.rootDir).ToList();
        }

        private IEnumerable<IArchiveFile> ReadDirectories(BinaryReaderX br, IsoDirEntry directory, string currentPath = "")
        {
            var directoryOffset = directory.body.lbaExtent.valueLe * SectorSize_;
            var internalOffset = 0x60;  // Skip self entry and parent entry directly

            while (br.PeekByte(directoryOffset + internalOffset) != 0)
            {
                br.BaseStream.Position = directoryOffset + internalOffset;

                var entry = ReadDirEntry(br);
                br.BaseStream.Position += 0xE;
                internalOffset = (int)(br.BaseStream.Position - directoryOffset);

                // Continue reading if the entry points to the current one
                if (entry.body.lbaExtent.valueLe == directory.body.lbaExtent.valueLe)
                    continue;

                // Read all sub directories and files of this directory
                if (entry.IsDirectory)
                {
                    foreach (var afi in ReadDirectories(br, entry, Path.Combine(currentPath, entry.body.fileName)))
                        yield return afi;
                    continue;
                }

                // Otherwise return file
                var subStream = new SubStream(br.BaseStream, entry.body.lbaExtent.valueLe * SectorSize_, entry.body.sizeExtent.valueLe);
                yield return new Ps2DiscArchiveFile(new ArchiveFileInfo
                {
                    FilePath = Path.Combine(currentPath, entry.body.fileName),
                    FileData = subStream
                }, entry);
            }
        }

        private IsoVolumeDescriptor ReadVolumeDescriptor(BinaryReaderX reader)
        {
            return new IsoVolumeDescriptor
            {
                type = reader.ReadByte(),
                magic = reader.ReadString(5),
                version = reader.ReadByte(),
                descriptorPrimary = ReadPrimaryDescriptor(reader)
            };
        }

        private IsoPrimaryVolumeDescriptor ReadPrimaryDescriptor(BinaryReaderX reader)
        {
            return new IsoPrimaryVolumeDescriptor
            {
                zero0 = reader.ReadByte(),
                systemId = reader.ReadString(0x20, Encoding.UTF8),
                volumeId = reader.ReadString(0x20, Encoding.UTF8),
                zero1 = reader.ReadInt64(),
                spaceSize = ReadIsoUInt32(reader),
                zero2 = reader.ReadBytes(0x20),
                setSize = ReadIsoUInt16(reader),
                seqCount = ReadIsoUInt16(reader),
                logicalBlockSize = ReadIsoUInt16(reader),
                pathTableSize = ReadIsoUInt32(reader),
                pathTable = ReadLbaPathTable(reader),
                rootDir = ReadDirEntry(reader),
                volumeSetId = reader.ReadString(0x80),
                publisherId = reader.ReadString(0x80),
                dataPreparerId = reader.ReadString(0x80),
                applicationId = reader.ReadString(0x80),
                copyrightFileId = reader.ReadString(0x26),
                abstractFileId = reader.ReadString(0x24),
                bibliographicFileId = reader.ReadString(0x25),
                createDateTime = ReadIsoDecDatetime(reader),
                modDateTime = ReadIsoDecDatetime(reader),
                expireDateTime = ReadIsoDecDatetime(reader),
                effectiveDateTime = ReadIsoDecDatetime(reader),
                fileStructureVersion = reader.ReadByte(),
                zero3 = reader.ReadByte()
            };
        }

        private IsoUInt32 ReadIsoUInt32(BinaryReaderX reader)
        {
            ByteOrder byteOrder = reader.ByteOrder;
            var result = new IsoUInt32();

            reader.ByteOrder = ByteOrder.LittleEndian;
            result.valueLe = reader.ReadUInt32();

            reader.ByteOrder = ByteOrder.BigEndian;
            result.valueBe = reader.ReadUInt32();

            reader.ByteOrder = byteOrder;
            return result;
        }

        private IsoUInt16 ReadIsoUInt16(BinaryReaderX reader)
        {
            ByteOrder byteOrder = reader.ByteOrder;
            var result = new IsoUInt16();

            reader.ByteOrder = ByteOrder.LittleEndian;
            result.valueLe = reader.ReadUInt16();

            reader.ByteOrder = ByteOrder.BigEndian;
            result.valueBe = reader.ReadUInt16();

            reader.ByteOrder = byteOrder;
            return result;
        }

        private IsoLbaPathTable ReadLbaPathTable(BinaryReaderX reader)
        {
            ByteOrder byteOrder = reader.ByteOrder;
            var result = new IsoLbaPathTable();

            reader.ByteOrder = ByteOrder.LittleEndian;
            result.lbaPathTableLe = reader.ReadUInt32();
            result.optLbaPathTableLe = reader.ReadUInt32();

            reader.ByteOrder = ByteOrder.BigEndian;
            result.lbaPathTableBe = reader.ReadUInt32();
            result.optLbaPathTableBe = reader.ReadUInt32();

            reader.ByteOrder = byteOrder;
            return result;
        }

        private IsoDirEntry ReadDirEntry(BinaryReaderX reader)
        {
            return new IsoDirEntry
            {
                length = reader.ReadByte(),
                body = ReadDirEntryContent(reader)
            };
        }

        private IsoDirEntryBody ReadDirEntryContent(BinaryReaderX reader)
        {
            var entry = new IsoDirEntryBody
            {
                attributeLength = reader.ReadByte(),
                lbaExtent = ReadIsoUInt32(reader),
                sizeExtent = ReadIsoUInt32(reader),
                dateTime = ReadIsoDatetime(reader),
                flags = reader.ReadByte(),
                unitSize = reader.ReadByte(),
                gapSize = reader.ReadByte(),
                seqCount = ReadIsoUInt16(reader),
                fileNameLength = reader.ReadByte()
            };

            entry.fileName = reader.ReadString(entry.fileNameLength, Encoding.UTF8);
            reader.SeekAlignment(2);

            return entry;
        }

        private IsoDatetime ReadIsoDatetime(BinaryReaderX reader)
        {
            return new IsoDatetime
            {
                year = reader.ReadByte(),
                month = reader.ReadByte(),
                day = reader.ReadByte(),
                hour = reader.ReadByte(),
                minute = reader.ReadByte(),
                second = reader.ReadByte(),
                timezone = reader.ReadByte()
            };
        }

        private IsoDecDateTime ReadIsoDecDatetime(BinaryReaderX reader)
        {
            return new IsoDecDateTime
            {
                year = reader.ReadString(4),
                month = reader.ReadString(2),
                day = reader.ReadString(2),
                hour = reader.ReadString(2),
                minute = reader.ReadString(2),
                second = reader.ReadString(2),
                microSecond = reader.ReadString(2),
                timeZone = reader.ReadByte()
            };
        }
    }
}
