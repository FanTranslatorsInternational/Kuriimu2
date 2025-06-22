using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    public class NCSD
    {
        private const int MediaSize_ = 0x200;
        private const int FirstPartitionOffset_ = 0x4000;

        private NcsdHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Parse NCCH partitions
            var result = new List<IArchiveFile>();
            for (var i = 0; i < 8; i++)
            {
                var partitionEntry = _header.partitionEntries[i];
                if (partitionEntry.length == 0)
                    continue;

                var name = GetPartitionName(i);
                var fileStream = new SubStream(input, (long)partitionEntry.offset * MediaSize_, (long)partitionEntry.length * MediaSize_);
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = fileStream,
                    PluginIds = [Guid.Parse("7d0177a6-1cab-44b3-bf22-39f5548d6cac")]
                }));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            // Update partition entries
            long partitionOffset = FirstPartitionOffset_;
            foreach (var file in files)
            {
                var partitionIndex = GetPartitionIndex(file.FilePath.GetName());
                var partitionEntry = _header.partitionEntries[partitionIndex];

                partitionEntry.offset = (int)(partitionOffset / MediaSize_);
                partitionEntry.length = (int)(file.FileSize / MediaSize_);

                output.Position = partitionOffset;
                file.WriteFileData(output);

                partitionOffset = output.Position;
            }

            // Store first NCCH header
            var firstNcchHeader = new byte[0x100];
            foreach (var partitionEntry in _header.partitionEntries)
            {
                if (partitionEntry.length != 0)
                {
                    var ncchStream = new SubStream(output, partitionEntry.offset * MediaSize_, partitionEntry.length * MediaSize_);
                    ncchStream.Read(firstNcchHeader, 0, 0x100);
                    break;
                }
            }

            _header.cardHeader.cardInfoHeader.firstNcchHeader = firstNcchHeader;

            output.Position = 0;

            using var bw = new BinaryWriterX(output);

            // Update NCSD size
            _header.ncsdSize = (int)(output.Length / MediaSize_);

            // Write NCSD header
            WriteHeader(_header, bw);

            // Pad until first partition
            bw.WritePadding(FirstPartitionOffset_ - 0x1200, 0xFF);
        }

        private string GetPartitionName(int partitionIndex)
        {
            switch (partitionIndex)
            {
                case 0:
                    return "GameData.cxi";

                case 1:
                    return "Manual.cfa";

                case 2:
                    return "DownloadPlay.cfa";

                case 6:
                    return "New3DSUpdateData.cfa";

                case 7:
                    return "UpdateData.cfa";

                default:
                    throw new InvalidOperationException($"Partition index {partitionIndex} is not associated.");
            }
        }

        private int GetPartitionIndex(string name)
        {
            switch (name)
            {
                case "GameData.cxi":
                    return 0;

                case "Manual.cfa":
                    return 1;

                case "DownloadPlay.cfa":
                    return 2;

                case "New3DSUpdateData.cfa":
                    return 6;

                case "UpdateData.cfa":
                    return 7;

                default:
                    throw new InvalidOperationException($"Partition name {name} is not associated.");
            }
        }

        private NcsdHeader ReadHeader(BinaryReaderX reader)
        {
            return new NcsdHeader
            {
                rsa2048 = reader.ReadBytes(0x100),
                magic = reader.ReadString(4),
                ncsdSize = reader.ReadInt32(),
                mediaId = reader.ReadInt64(),
                partitionFsType = reader.ReadBytes(8),
                partitionCryptType = reader.ReadBytes(8),
                partitionEntries = ReadPartitionEntries(reader, 8),
                cardHeader = ReadCardHeader(reader)
            };
        }

        private NcsdPartitionEntry[] ReadPartitionEntries(BinaryReaderX reader, int count)
        {
            var result = new NcsdPartitionEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadPartitionEntry(reader);

            return result;
        }

        private NcsdPartitionEntry ReadPartitionEntry(BinaryReaderX reader)
        {
            return new NcsdPartitionEntry
            {
                offset = reader.ReadInt32(),
                length = reader.ReadInt32()
            };
        }

        private NcsdCardHeader ReadCardHeader(BinaryReaderX reader)
        {
            return new NcsdCardHeader
            {
                exHeaderHash = reader.ReadBytes(0x20),
                additionalHeaderSize = reader.ReadInt32(),
                sectorZeroOffset = reader.ReadInt32(),
                partitionFlags = reader.ReadBytes(0x8),
                partitionIds = ReadLongs(reader, 0x8),
                reserved1 = reader.ReadBytes(0x20),
                reserved2 = reader.ReadBytes(0xE),
                unk1 = reader.ReadByte(),
                unk2 = reader.ReadByte(),
                cardInfoHeader = ReadCardInfoHeader(reader)
            };
        }

        private long[] ReadLongs(BinaryReaderX reader, int count)
        {
            var result = new long[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt64();

            return result;
        }

        private NcsdCardInfoHeader ReadCardInfoHeader(BinaryReaderX reader)
        {
            return new NcsdCardInfoHeader
            {
                card2WriteAddress = reader.ReadInt32(),
                cardBitMask = reader.ReadInt32(),
                reserved1 = reader.ReadBytes(0x108),
                titleVersion = reader.ReadInt16(),
                cardRevision = reader.ReadInt16(),
                reserved2 = reader.ReadBytes(0xCEC),
                cardSeedKeyY = reader.ReadBytes(0x10),
                encryptedCardSeed = reader.ReadBytes(0x10),
                cardSeedAesMac = reader.ReadBytes(0x10),
                cardSeedNonce = reader.ReadBytes(0xC),
                reserved3 = reader.ReadBytes(0xC4),
                firstNcchHeader = reader.ReadBytes(0x100)
            };
        }

        private void WriteHeader(NcsdHeader header, BinaryWriterX writer)
        {
            writer.Write(header.rsa2048);
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.ncsdSize);
            writer.Write(header.mediaId);
            writer.Write(header.partitionFsType);
            writer.Write(header.partitionCryptType);

            WritePartitionEntries(header.partitionEntries, writer);
            WriteCardHeader(header.cardHeader, writer);
        }

        private void WritePartitionEntries(NcsdPartitionEntry[] entries, BinaryWriterX writer)
        {
            foreach (NcsdPartitionEntry entry in entries)
                WritePartitionEntry(entry, writer);
        }

        private void WritePartitionEntry(NcsdPartitionEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.length);
        }

        private void WriteCardHeader(NcsdCardHeader header, BinaryWriterX writer)
        {
            writer.Write(header.exHeaderHash);
            writer.Write(header.additionalHeaderSize);
            writer.Write(header.sectorZeroOffset);
            writer.Write(header.partitionFlags);
            WriteLongs(header.partitionIds, writer);
            writer.Write(header.reserved1);
            writer.Write(header.reserved2);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            WriteCardInfoHeader(header.cardInfoHeader, writer);
        }

        private void WriteLongs(long[] entries, BinaryWriterX writer)
        {
            foreach (long entry in entries)
                writer.Write(entry);
        }

        private void WriteCardInfoHeader(NcsdCardInfoHeader header, BinaryWriterX writer)
        {
            writer.Write(header.card2WriteAddress);
            writer.Write(header.cardBitMask);
            writer.Write(header.reserved1);
            writer.Write(header.titleVersion);
            writer.Write(header.cardRevision);
            writer.Write(header.reserved2);
            writer.Write(header.cardSeedKeyY);
            writer.Write(header.encryptedCardSeed);
            writer.Write(header.cardSeedAesMac);
            writer.Write(header.cardSeedNonce);
            writer.Write(header.reserved3);
            writer.Write(header.firstNcchHeader);
        }
    }
}
