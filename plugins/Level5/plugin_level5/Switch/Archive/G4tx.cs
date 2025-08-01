using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Extensions;
using Kryptography.Checksum.Crc;

namespace plugin_level5.Switch.Archive
{
    // Hash: Crc32.Default
    public class G4tx
    {
        private const int HeaderSize_ = 0x60;
        private const int EntrySize_ = 0x30;
        private const int SubEntrySize_ = 0x18;

        private G4txHeader _header;
        private IList<G4txEntry> _entries;
        private IList<G4txSubEntry> _subEntries;
        private IList<byte> _ids;

        public List<G4txArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read entries
            _entries = ReadEntries(br, _header.textureCount);
            _subEntries = ReadSubEntries(br, _header.subTextureCount);
            br.SeekAlignment();

            // Skip hashes
            _ = ReadUnsignedIntegers(br, _header.totalCount);

            // Read ids
            _ids = br.ReadBytes(_header.totalCount);
            br.SeekAlignment(4);

            // Prepare string reader
            var nxtchBase = (_header.headerSize + _header.tableSize + 0xF) & ~0xF;
            var stringSize = nxtchBase - input.Position;
            var stringStream = new SubStream(input, input.Position, stringSize);
            using var stringBr = new BinaryReaderX(stringStream);

            // Read string offsets
            var stringOffsets = ReadShorts(br, _header.totalCount);

            // Add files
            // TODO: Check if name is set by order of entries or ID
            var result = new List<G4txArchiveFile>();
            var subEntryId = _header.textureCount;
            for (var i = 0; i < _header.textureCount; i++)
            {
                var entry = _entries[i];

                // Prepare base information
                stringStream.Position = stringOffsets[i];
                var name = stringBr.ReadNullTerminatedString();

                var fileStream = new SubStream(input, nxtchBase + entry.nxtchOffset, entry.nxtchSize);

                // Prepare sub entries
                var subEntries = new List<G4txSubTextureEntry>();
                foreach (var unkEntry in _subEntries.Where(x => x.entryId == i))
                {
                    stringStream.Position = stringOffsets[subEntryId];
                    var subName = stringBr.ReadNullTerminatedString();

                    subEntries.Add(new G4txSubTextureEntry(_ids[subEntryId++], unkEntry, subName));
                }

                var fileInfo = new ArchiveFileInfo
                {
                    FileData = fileStream,
                    FilePath = name + ".nxtch",
                    PluginIds = [Guid.Parse("89222f8f-a345-45ed-9b79-e9e873bda1e9")]
                };

                result.Add(new G4txArchiveFile(fileInfo, entry, _ids[i], subEntries));
            }

            return result;
        }

        public void Save(Stream output, List<G4txArchiveFile> files)
        {
            var crc = Crc32.Crc32B;

            using var bw = new BinaryWriterX(output);
            using var br = new BinaryReaderX(output);

            // Calculate offsets
            var subEntryCount = files.Sum(x => x.Entries.Count);

            var entryOffset = HeaderSize_;
            var subEntryOffset = entryOffset + files.Count * EntrySize_;
            var hashOffset = (subEntryOffset + subEntryCount * SubEntrySize_ + 0xF) & ~0xF;
            var idOffset = hashOffset + (files.Count + subEntryCount) * 4;
            var stringOffset = (idOffset + (files.Count + subEntryCount) + 0x3) & ~0x3;
            var stringContentOffset = (stringOffset + (files.Count + subEntryCount) * 2 + 0x7) & ~0x3;
            var dataOffset = (stringContentOffset + files
                .Sum(x => x.FilePath.GetNameWithoutExtension().Length + 1 + x.Entries.Sum(y => y.Name.Length + 1)) + 0xF) & ~0xF;

            // Write files
            var dataPosition = dataOffset;
            foreach (G4txArchiveFile file in files)
            {
                output.Position = dataPosition;
                var writtenSize = file.WriteFileData(output, false);

                // Update file entry
                output.Position = dataPosition;
                var nxtchHeader = ReadNxtchHeader(br);

                file.Entry.nxtchOffset = dataPosition - dataOffset;
                file.Entry.nxtchSize = (int)(output.Length - dataPosition);
                file.Entry.width = (short)nxtchHeader.width;
                file.Entry.height = (short)nxtchHeader.height;

                dataPosition = (int)((dataPosition + writtenSize + 0xF) & ~0xF);
            }

            // Write strings
            var stringContentPosition = stringContentOffset;

            var names = files.Select(x => x.FilePath.GetNameWithoutExtension())
                .Concat(files.SelectMany(x => x.Entries.Select(y => y.Name))).ToArray();
            var stringOffsets = new List<short>();
            foreach (var name in names)
            {
                stringOffsets.Add((short)(stringContentPosition - stringOffset));

                output.Position = stringContentPosition;
                bw.WriteString(name, Encoding.ASCII);

                stringContentPosition += name.Length + 1;
            }

            // Write string offsets
            output.Position = stringOffset;
            WriteShorts(stringOffsets, bw);

            // Write ids
            var ids = files.Select(x => x.Id).Concat(files.SelectMany(x => x.Entries.Select(y => y.Id))).ToArray();

            output.Position = idOffset;
            bw.Write(ids);

            // Write hashes
            var hashes = names.Select(crc.ComputeValue).ToArray();

            output.Position = hashOffset;
            WriteUnsignedIntegers(hashes, bw);

            // Write sub entries
            var subEntries = files.SelectMany(x => x.Entries.Select(y => y.EntryEntry)).ToArray();

            output.Position = subEntryOffset;
            WriteSubEntries(subEntries, bw);

            // Write entries
            output.Position = entryOffset;
            WriteEntries(files, bw);

            // Write header
            _header.textureCount = (short)files.Count;
            _header.tableSize = (stringContentPosition - HeaderSize_ + 3) & ~3;
            _header.textureDataSize = (int)output.Length - dataOffset;
            _header.subTextureCount = (byte)subEntries.Length;
            _header.totalCount = (short)(_header.textureCount + _header.subTextureCount);

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private G4txHeader ReadHeader(BinaryReaderX reader)
        {
            return new G4txHeader
            {
                magic = reader.ReadString(4),
                headerSize = reader.ReadInt16(),
                fileType = reader.ReadInt16(),
                unk1 = reader.ReadInt32(),
                tableSize = reader.ReadInt32(),
                zeroes = reader.ReadBytes(0x10),
                textureCount = reader.ReadInt16(),
                totalCount = reader.ReadInt16(),
                unk2 = reader.ReadByte(),
                subTextureCount = reader.ReadByte(),
                unk3 = reader.ReadInt16(),
                unk4 = reader.ReadInt32(),
                textureDataSize = reader.ReadInt32(),
                unk5 = reader.ReadInt64(),
                unk6 = reader.ReadBytes(0x28)
            };
        }

        private G4txEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new G4txEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private G4txEntry ReadEntry(BinaryReaderX reader)
        {
            return new G4txEntry
            {
                unk1 = reader.ReadInt32(),
                nxtchOffset = reader.ReadInt32(),
                nxtchSize = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadInt32(),
                unk4 = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                const2 = reader.ReadInt32(),
                unk5 = reader.ReadBytes(0x10)
            };
        }

        private G4txSubEntry[] ReadSubEntries(BinaryReaderX reader, int count)
        {
            var result = new G4txSubEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadSubEntry(reader);

            return result;
        }

        private G4txSubEntry ReadSubEntry(BinaryReaderX reader)
        {
            return new G4txSubEntry
            {
                entryId = reader.ReadInt16(),
                unk1 = reader.ReadInt16(),
                x = reader.ReadInt16(),
                y = reader.ReadInt16(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadInt32(),
                unk4 = reader.ReadInt32()
            };
        }

        private uint[] ReadUnsignedIntegers(BinaryReaderX reader, int count)
        {
            var result = new uint[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadUInt32();

            return result;
        }

        private short[] ReadShorts(BinaryReaderX reader, int count)
        {
            var result = new short[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt16();

            return result;
        }

        private NxtchHeader ReadNxtchHeader(BinaryReaderX reader)
        {
            return new NxtchHeader
            {
                magic = reader.ReadString(8),
                textureDataSize = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                width = reader.ReadInt32(),
                height = reader.ReadInt32(),
                unk3 = reader.ReadInt32(),
                unk4 = reader.ReadInt32(),
                format = reader.ReadInt32(),
                mipMapCount = reader.ReadInt32(),
                textureDataSize2 = reader.ReadInt32()
            };
        }

        private void WriteHeader(G4txHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.headerSize);
            writer.Write(header.fileType);
            writer.Write(header.unk1);
            writer.Write(header.tableSize);
            writer.Write(header.zeroes);
            writer.Write(header.textureCount);
            writer.Write(header.totalCount);
            writer.Write(header.unk2);
            writer.Write(header.subTextureCount);
            writer.Write(header.unk3);
            writer.Write(header.unk4);
            writer.Write(header.textureDataSize);
            writer.Write(header.unk5);
            writer.Write(header.unk6);
        }

        private void WriteEntries(IList<G4txArchiveFile> entries, BinaryWriterX writer)
        {
            foreach (G4txArchiveFile entry in entries)
                WriteEntry(entry.Entry, writer);
        }

        private void WriteEntry(G4txEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.unk1);
            writer.Write(entry.nxtchOffset);
            writer.Write(entry.nxtchSize);
            writer.Write(entry.unk2);
            writer.Write(entry.unk3);
            writer.Write(entry.unk4);
            writer.Write(entry.width);
            writer.Write(entry.height);
            writer.Write(entry.const2);
            writer.Write(entry.unk5);
        }

        private void WriteSubEntries(G4txSubEntry[] entries, BinaryWriterX writer)
        {
            foreach (G4txSubEntry entry in entries)
                WriteSubEntry(entry, writer);
        }

        private void WriteSubEntry(G4txSubEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.entryId);
            writer.Write(entry.unk1);
            writer.Write(entry.x);
            writer.Write(entry.y);
            writer.Write(entry.width);
            writer.Write(entry.height);
            writer.Write(entry.unk2);
            writer.Write(entry.unk3);
            writer.Write(entry.unk4);
        }

        private void WriteUnsignedIntegers(IList<uint> entries, BinaryWriterX writer)
        {
            foreach (uint entry in entries)
                writer.Write(entry);
        }

        private void WriteShorts(IList<short> entries, BinaryWriterX writer)
        {
            foreach (short entry in entries)
                writer.Write(entry);
        }
    }
}
