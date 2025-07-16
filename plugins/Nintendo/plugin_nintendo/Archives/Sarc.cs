using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Kryptography.Checksum;

namespace plugin_nintendo.Archives
{
    class Sarc
    {
        private const int HeaderSize = 0x14;
        private const int SfatHeaderSize = 0xC;
        private const int SfatEntrySize = 0x10;
        private const int SfntHeaderSize = 0x8;

        private ByteOrder _byteOrder;
        private SarcHeader _header;
        private SfatHeader _sfatHeader;
        private SfntHeader? _sfntHeader;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Determine byte order
            input.Position = 0x6;
            br.ByteOrder = _byteOrder = (ByteOrder)br.ReadUInt16();

            // Read header
            input.Position = 0;
            _header = ReadHeader(br);

            // Read entries
            _sfatHeader = ReadSfatHeader(br);
            var entries = ReadEntries(br, _sfatHeader.entryCount);

            // Read names
            BinaryReaderX nameBr = null;
            if (entries.Any(x => (x.Flags & 0x100) > 0))
            {
                _sfntHeader = ReadSfntHeader(br);
                var nameStream = new SubStream(input, input.Position, _header.dataOffset - input.Position);
                nameBr = new BinaryReaderX(nameStream);
            }

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, _header.dataOffset + entry.startOffset, entry.endOffset - entry.startOffset);
                var magic = SarcSupport.DetermineMagic(fileStream);

                var name = $"{entry.nameHash:X8}{SarcSupport.DetermineExtension(magic)}";
                if (nameBr != null)
                {
                    nameBr.BaseStream.Position = entry.FntOffset;
                    name = nameBr.ReadNullTerminatedString();
                }

                result.Add(new SarcArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = fileStream
                }, magic, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files, bool isCompressed)
        {
            var simpleHash = new Simple(_sfatHeader.hashMultiplier);

            using var bw = new BinaryWriterX(output, true, _byteOrder);

            var sortedFiles = files.Cast<SarcArchiveFile>().OrderBy(x => _sfntHeader == null ? x.Entry.nameHash : simpleHash.ComputeValue(x.FilePath.ToRelative().FullName)).ToArray();

            // Calculate offsets
            var sfatOffset = HeaderSize;
            var sfntOffset = sfatOffset + SfatHeaderSize + files.Count * SfatEntrySize;
            var dataOffset = _sfntHeader == null
                ? sfntOffset + SfntHeaderSize
                : sfntOffset + SfntHeaderSize + files.Sum(x => (x.FilePath.ToRelative().FullName.Length + 4) & ~3);

            var alignment = sortedFiles.Max(x => SarcSupport.DetermineAlignment(x, _byteOrder, isCompressed));
            var alignedDataOffset = (dataOffset + alignment - 1) & ~(alignment - 1);

            // Write files
            var entries = new List<SfatEntry>();
            var strings = new List<string>();

            var stringPosition = 0;
            var dataPosition = alignedDataOffset;
            foreach (var file in sortedFiles)
            {
                // Write file data
                alignment = SarcSupport.DetermineAlignment(file, _byteOrder, isCompressed);
                var alignedDataPosition = (dataPosition + alignment - 1) & ~(alignment - 1);

                output.Position = alignedDataPosition;
                var writtenSize = file.WriteFileData(output, true);

                // Add entry
                entries.Add(new SfatEntry
                {
                    startOffset = alignedDataPosition - alignedDataOffset,
                    endOffset = (int)(alignedDataPosition + writtenSize - alignedDataOffset),
                    Flags = (short)(_sfntHeader == null ? 0 : 0x100),
                    FntOffset = (short)(_sfntHeader == null ? 0 : stringPosition),
                    nameHash = _sfntHeader == null ? file.Entry.nameHash : simpleHash.ComputeValue(file.FilePath.ToRelative().FullName)
                });

                // Add string
                strings.Add(file.FilePath.ToRelative().FullName);

                dataPosition = (int)(alignedDataPosition + writtenSize);
                stringPosition += (file.FilePath.ToRelative().FullName.Length + 4) & ~3;
            }

            // Write SFNT
            if (_sfntHeader.HasValue)
            {
                output.Position = sfntOffset;
                WriteSfntHeader(_sfntHeader.Value, bw);

                foreach (var s in strings)
                {
                    bw.WriteString(s, Encoding.ASCII);
                    bw.WriteAlignment(4);
                }
            }

            // Write SFAT
            var sfatHeader = new SfatHeader
            {
                magic = "SFAT",
                headerSize = 0xC,
                entryCount = (short)files.Count,
                hashMultiplier = _sfatHeader.hashMultiplier
            };

            output.Position = sfatOffset;
            WriteSfatHeader(sfatHeader, bw);
            WriteEntries(entries, bw);

            // Write header
            var header = new SarcHeader
            {
                magic = "SARC",
                headerSize = 0x14,
                byteOrder = (ushort)_byteOrder,
                dataOffset = alignedDataOffset,
                fileSize = (int)output.Length,
                unk1 = _header.unk1
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private SarcHeader ReadHeader(BinaryReaderX reader)
        {
            return new SarcHeader
            {
                magic = reader.ReadString(4),
                headerSize = reader.ReadInt16(),
                byteOrder = reader.ReadUInt16(),
                fileSize = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                unk1 = reader.ReadInt32()
            };
        }

        private SfatHeader ReadSfatHeader(BinaryReaderX reader)
        {
            return new SfatHeader
            {
                magic = reader.ReadString(4),
                headerSize = reader.ReadInt16(),
                entryCount = reader.ReadInt16(),
                hashMultiplier = reader.ReadUInt32()
            };
        }

        private SfatEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new SfatEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private SfatEntry ReadEntry(BinaryReaderX reader)
        {
            return new SfatEntry
            {
                nameHash = reader.ReadUInt32(),
                fntFlagOffset = reader.ReadUInt32(),
                startOffset = reader.ReadInt32(),
                endOffset = reader.ReadInt32(),
            };
        }

        private SfntHeader ReadSfntHeader(BinaryReaderX reader)
        {
            return new SfntHeader
            {
                magic = reader.ReadString(4),
                headerSize = reader.ReadInt16(),
                zero0 = reader.ReadInt16()
            };
        }

        private void WriteHeader(SarcHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.headerSize);

            writer.ByteOrder = ByteOrder.BigEndian;
            writer.Write(header.byteOrder);

            writer.ByteOrder = (ByteOrder)header.byteOrder;
            writer.Write(header.fileSize);
            writer.Write(header.dataOffset);
            writer.Write(header.unk1);
        }

        private void WriteSfatHeader(SfatHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.headerSize);
            writer.Write(header.entryCount);
            writer.Write(header.hashMultiplier);
        }

        private void WriteEntries(IList<SfatEntry> entries, BinaryWriterX writer)
        {
            foreach (SfatEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(SfatEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.nameHash);
            writer.Write(entry.fntFlagOffset);
            writer.Write(entry.startOffset);
            writer.Write(entry.endOffset);
        }

        private void WriteSfntHeader(SfntHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.headerSize);
            writer.Write(header.zero0);
        }
    }
}
