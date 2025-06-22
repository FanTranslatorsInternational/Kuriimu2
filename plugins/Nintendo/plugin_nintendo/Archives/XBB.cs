using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    public class Xbb
    {
        private const int HeaderSize_ = 0x20;
        private const int EntrySize_ = 0x10;
        private const int HashEntrySize_ = 0x8;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);
            br.SeekAlignment(0x20);

            // Read entries
            var entries = ReadFileEntries(br, header.entryCount);

            // Read hash entries
            _ = ReadHashEntries(br, header.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.offset, entry.size);

                br.BaseStream.Position = entry.nameOffset;
                var name = br.ReadNullTerminatedString();

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = fileStream
                }));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var entryPosition = HeaderSize_;
            var hashEntryPosition = entryPosition + files.Count * EntrySize_;
            var namePosition = hashEntryPosition + files.Count * HashEntrySize_;

            using var bw = new BinaryWriterX(output);

            // Write names
            bw.BaseStream.Position = namePosition;

            var nameDictionary = new Dictionary<UPath, int>();
            foreach (var file in files)
            {
                if (!nameDictionary.ContainsKey(file.FilePath))
                    nameDictionary.Add(file.FilePath, (int)bw.BaseStream.Position);

                bw.WriteString(file.FilePath.ToRelative().FullName, Encoding.ASCII, false);
            }

            var dataPosition = (bw.BaseStream.Position + 0x7F) & ~0x7F;

            // Write files
            bw.BaseStream.Position = dataPosition;

            var xbbHash = new Kryptography.Checksum.Xbb();
            var fileEntries = new List<XbbFileEntry>();
            var hashEntries = new List<XbbHashEntry>();
            foreach (var file in files)
            {
                var offset = bw.BaseStream.Position;
                var writtenSize = file.WriteFileData(bw.BaseStream);
                bw.WriteAlignment(0x80);

                var hash = xbbHash.ComputeValue(file.FilePath.ToRelative().FullName);
                fileEntries.Add(new XbbFileEntry
                {
                    offset = (int)offset,
                    size = (int)writtenSize,
                    nameOffset = nameDictionary[file.FilePath],
                    hash = hash
                });

                hashEntries.Add(new XbbHashEntry
                {
                    hash = hash,
                    index = fileEntries.Count - 1
                });
            }

            // Write file entries
            bw.BaseStream.Position = entryPosition;
            WriteFileEntries(fileEntries, bw);

            // Write hash entries
            bw.BaseStream.Position = hashEntryPosition;
            WriteHashEntries(hashEntries.OrderBy(x => x.hash).ToArray(), bw);

            // Write header
            var header = new XbbHeader
            {
                magic = "XBB",
                version = 1,
                entryCount = files.Count
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);

            bw.WriteAlignment(0x20);
        }

        private XbbHeader ReadHeader(BinaryReaderX reader)
        {
            return new XbbHeader
            {
                magic = reader.ReadString(3),
                version = reader.ReadByte(),
                entryCount = reader.ReadInt32()
            };
        }

        private XbbFileEntry[] ReadFileEntries(BinaryReaderX reader, int count)
        {
            var result = new XbbFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private XbbFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new XbbFileEntry
            {
                offset = reader.ReadInt32(),
                size = reader.ReadInt32(),
                nameOffset = reader.ReadInt32(),
                hash = reader.ReadUInt32()
            };
        }

        private XbbHashEntry[] ReadHashEntries(BinaryReaderX reader, int count)
        {
            var result = new XbbHashEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadHashEntry(reader);

            return result;
        }

        private XbbHashEntry ReadHashEntry(BinaryReaderX reader)
        {
            return new XbbHashEntry
            {
                hash = reader.ReadUInt32(),
                index = reader.ReadInt32()
            };
        }

        private void WriteHeader(XbbHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.entryCount);
        }

        private void WriteFileEntries(IList<XbbFileEntry> entries, BinaryWriterX writer)
        {
            foreach (XbbFileEntry entry in entries)
                WriteFileEntry(entry, writer);
        }

        private void WriteFileEntry(XbbFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
            writer.Write(entry.nameOffset);
            writer.Write(entry.hash);
        }

        private void WriteHashEntries(IList<XbbHashEntry> entries, BinaryWriterX writer)
        {
            foreach (XbbHashEntry entry in entries)
                WriteHashEntry(entry, writer);
        }

        private void WriteHashEntry(XbbHashEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.hash);
            writer.Write(entry.index);
        }
    }
}
