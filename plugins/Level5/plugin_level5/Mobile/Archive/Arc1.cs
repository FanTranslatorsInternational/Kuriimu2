using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_level5.Mobile.Archive
{
    public class Arc1
    {
        private const int HeaderSize_ = 20;
        private const int EntrySize_ = 12;

        private Arc1Header _header;

        public List<Arc1ArchiveFile> Load(Stream input)
        {
            using var headerBr = new BinaryReaderX(new Arc1CryptoStream(input, 0), true);

            // Read header
            _header = ReadHeader(headerBr);

            // Prepare file info stream
            var infoStream = new Arc1CryptoStream(new SubStream(input, _header.entryOffset, _header.entrySize), (uint)_header.entryOffset);
            using var infoBr = new BinaryReaderX(infoStream);

            // Read entries
            var entryCount = infoBr.ReadInt32();
            var entries = ReadEntries(infoBr, entryCount);

            // Add files
            var result = new List<Arc1ArchiveFile>();
            foreach (var entry in entries)
            {
                infoStream.Position = entry.nameOffset;
                var name = infoBr.ReadNullTerminatedString();

                var fileStream = new SubStream(input, entry.offset, entry.size);
                var fileInfo = new ArchiveFileInfo
                {
                    FileData = Path.GetExtension(name) != ".mp4" ? new Arc1CryptoStream(fileStream, (uint)entry.offset) : fileStream,
                    FilePath = name
                };

                result.Add(new Arc1ArchiveFile(fileInfo));
            }

            return result;
        }

        public void Save(Stream output, List<Arc1ArchiveFile> files)
        {
            // Calculate offsets
            var dataOffset = HeaderSize_;
            var entryOffset = dataOffset + files.Sum(x => x.FileSize);
            var stringOffset = entryOffset + 4 + files.Count * EntrySize_;
            var totalSize = stringOffset + files.Sum(x => x.FilePath.ToRelative().FullName.Length + 1);

            // Prepare output stream
            output.SetLength(totalSize);

            // Write files
            var dataPosition = dataOffset;
            var stringPosition = stringOffset - entryOffset;

            var entries = new List<Arc1FileEntry>();
            foreach (Arc1ArchiveFile file in files)
            {
                // Write file data
                Stream outputRegion = new SubStream(output, dataPosition, file.FileSize);

                Stream finalStream = file.GetFinalStream();
                if (finalStream is Arc1CryptoStream cryptoStream && cryptoStream.OriginalPosition == (uint)dataPosition)
                    cryptoStream.BaseStream.CopyTo(outputRegion);
                else
                {
                    outputRegion = new Arc1CryptoStream(outputRegion, (uint)dataPosition);
                    finalStream.CopyTo(outputRegion);
                }

                // Add entry
                entries.Add(new Arc1FileEntry
                {
                    nameOffset = (int)stringPosition,
                    offset = dataPosition,
                    size = (int)file.FileSize
                });

                dataPosition += (int)file.FileSize;
                stringPosition += file.FilePath.ToRelative().FullName.Length + 1;
            }

            // Write entry information
            var infoStream = new Arc1CryptoStream(new SubStream(output, entryOffset, totalSize - entryOffset), (uint)entryOffset);

            using var infoBw = new BinaryWriterX(infoStream);

            infoBw.Write(files.Count);
            WriteEntries(entries, infoBw);

            foreach (var name in files.Select(x => x.FilePath.ToRelative().FullName))
                infoBw.WriteString(name);

            // Write header
            var headerStream = new Arc1CryptoStream(new SubStream(output, 0, HeaderSize_), 0);
            using var headerBw = new BinaryWriterX(headerStream);

            _header.entryOffset = (int)entryOffset;
            _header.entrySize = (int)(totalSize - entryOffset);
            _header.fileSize = (int)totalSize;

            WriteHeader(_header, headerBw);
        }

        private Arc1Header ReadHeader(BinaryReaderX reader)
        {
            return new Arc1Header
            {
                magic = reader.ReadString(4),
                fileSize = reader.ReadInt32(),
                entryOffset = reader.ReadInt32(),
                entrySize = reader.ReadInt32(),
                unk1 = reader.ReadInt32()
            };
        }

        private Arc1FileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new Arc1FileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private Arc1FileEntry ReadEntry(BinaryReaderX reader)
        {
            return new Arc1FileEntry
            {
                nameOffset = reader.ReadInt32(),
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private void WriteHeader(Arc1Header header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileSize);
            writer.Write(header.entryOffset);
            writer.Write(header.entrySize);
            writer.Write(header.unk1);
        }

        private void WriteEntries(IList<Arc1FileEntry> entries, BinaryWriterX writer)
        {
            foreach (Arc1FileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(Arc1FileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.nameOffset);
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
