using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using plugin_konami.Compression;

namespace plugin_konami.Archives
{
    class Tarc
    {
        private TarcHeader _header;
        private bool _hasNames;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);
            _hasNames = _header.nameOffset != 0;

            // Read entries
            input.Position = _header.entryOffset;
            var entries = ReadEntries(br, _header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.fileOffset, entry.compSize);

                var fileName = $"{i:00000000}.bin";
                if (_hasNames)
                {
                    input.Position = entry.nameOffset;
                    fileName = br.ReadNullTerminatedString();
                }

                result.Add(CreateAfi(subStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Distinct strings
            var stringMap = new Dictionary<string, int>();

            var stringPosition = 0;
            foreach (var name in files.Select(x => x.FilePath.ToRelative().FullName))
            {
                if (!stringMap.TryAdd(name, stringPosition))
                    continue;

                stringPosition += name.Length + 1;
            }

            // Calculate offsets
            var entryOffset = 0x30;
            var entrySize = files.Count * 0x20;
            var stringOffset = _hasNames ? entryOffset + entrySize : 0;
            var fileOffset = _hasNames ? (stringOffset + stringMap.Keys.Sum(x => x.Length + 1) + 0xF) & ~0xF : entryOffset + entrySize;

            // Write files
            var entries = new List<TarcEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<TarcArchiveFile>())
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output, true);

                // Update entry
                file.Entry.fileOffset = filePosition;
                file.Entry.compSize = (int)writtenSize;
                file.Entry.decompSize = (int)file.FileSize;
                file.Entry.nameOffset = _hasNames ? stringMap[file.FilePath.ToRelative().FullName] + stringOffset : 0;
                entries.Add(file.Entry);

                filePosition += (int)((writtenSize + 0xF) & ~0xF);
            }

            // Write strings
            if (_hasNames)
            {
                output.Position = stringOffset;
                foreach (var name in stringMap.Keys)
                    bw.WriteString(name, Encoding.ASCII, false);
            }
            var stringSecSize = _hasNames ? (int)output.Position - stringOffset : 0;

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            output.Position = 0;

            _header.fileCount = files.Count;
            _header.entryOffset = entryOffset;
            _header.nameOffset = stringOffset;
            _header.entrySecSize = files.Count * 0x20;
            _header.nameSecSize = stringSecSize;
            _header.fileSize = (int)output.Length;

            WriteHeader(_header, bw);
        }

        private IArchiveFile CreateAfi(Stream file, string name, TarcEntry entry)
        {
            if (entry.compSize != 0 && entry.compSize != entry.decompSize)
            {
                var method = NintendoCompressor.PeekCompressionMethod(file);
                return new TarcArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = name,
                    FileData = file,
                    Compression = NintendoCompressor.GetConfiguration(method),
                    DecompressedSize = entry.decompSize
                }, entry);
            }

            return new TarcArchiveFile(new ArchiveFileInfo
            {
                FilePath = name,
                FileData = file
            }, entry);
        }

        private TarcHeader ReadHeader(BinaryReaderX reader)
        {
            return new TarcHeader
            {
                magic = reader.ReadString(4),
                fileSize = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                entryOffset = reader.ReadInt32(),
                entrySecSize = reader.ReadInt32(),
                nameOffset = reader.ReadInt32(),
                nameSecSize = reader.ReadInt32(),
                unk3 = reader.ReadInt32()
            };
        }

        private TarcEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new TarcEntry[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadEntry(reader);
                reader.SeekAlignment();
            }

            return result;
        }

        private TarcEntry ReadEntry(BinaryReaderX reader)
        {
            return new TarcEntry
            {
                unk1 = reader.ReadInt32(),
                nameOffset = reader.ReadInt32(),
                fileOffset = reader.ReadInt32(),
                decompSize = reader.ReadInt32(),
                compSize = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private void WriteHeader(TarcHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileSize);
            writer.Write(header.fileCount);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.entryOffset);
            writer.Write(header.entrySecSize);
            writer.Write(header.nameOffset);
            writer.Write(header.nameSecSize);
            writer.Write(header.unk3);
        }

        private void WriteEntries(IList<TarcEntry> entries, BinaryWriterX writer)
        {
            foreach (TarcEntry entry in entries)
            {
                WriteEntry(entry, writer);
                writer.WriteAlignment(0x10);
            }
        }

        private void WriteEntry(TarcEntry entry, in BinaryWriterX writer)
        {
            writer.Write(entry.unk1);
            writer.Write(entry.nameOffset);
            writer.Write(entry.fileOffset);
            writer.Write(entry.decompSize);
            writer.Write(entry.compSize);
            writer.Write(entry.unk2);
        }
    }
}
