using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Extensions;
using Kryptography.Checksum.Crc;

namespace plugin_level5.N3DS.Archive
{
    // Game: Inazuma 3 Ogre Team
    public class B123
    {
        private const int HeaderSize_ = 72;
        private const int DirectoryEntrySize_ = 24;
        private const int DirectoryHashSize_ = 4;
        private const int FileEntrySize_ = 16;

        private B123Header _header;

        public List<B123ArchiveFile> Load(Stream input)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var br = new BinaryReaderX(input, Encoding.GetEncoding("Shift-JIS"), true);

            // Read header
            _header = ReadHeader(br);

            // Read directory entries
            br.BaseStream.Position = _header.directoryEntriesOffset;
            var directoryEntries = ReadDirectoryEntries(br, _header.directoryEntriesCount);

            // Read file entry table
            br.BaseStream.Position = _header.fileEntriesOffset;
            var entries = ReadFileEntries(br, _header.fileEntriesCount);

            // Add Files
            var result = new List<B123ArchiveFile>();
            foreach (var directory in directoryEntries)
            {
                var filesInDirectory = entries.Skip(directory.firstFileIndex).Take(directory.fileCount);
                foreach (var file in filesInDirectory)
                {
                    var fileStream = new SubStream(input, _header.dataOffset + file.fileOffset, file.fileSize);

                    br.BaseStream.Position = _header.nameOffset + directory.fileNameStartOffset + file.nameOffsetInFolder;
                    string fileName = br.ReadNullTerminatedString();

                    br.BaseStream.Position = _header.nameOffset + directory.directoryNameStartOffset;
                    string directoryName = br.ReadNullTerminatedString();

                    result.Add(CreateAfi(fileStream, directoryName + fileName, file));
                }
            }

            return result;
        }

        public void Save(Stream output, List<B123ArchiveFile> files)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Build directory, file, and name tables
            BuildTables(files, out var directoryEntries, out var directoryHashes, out var fileEntries, out var nameStream);

            // -- Write file --

            using var bw = new BinaryWriterX(output);
            bw.BaseStream.Position = HeaderSize_;

            _header.dataOffset = (int)(HeaderSize_ +
                                       directoryEntries.Count * DirectoryEntrySize_ +
                                       directoryHashes.Count * DirectoryHashSize_ +
                                       fileEntries.Count * FileEntrySize_ +
                                       nameStream.Length + 3) & ~3;

            // Write file data
            var fileOffset = 0u;
            var fileIndex = 1;
            foreach (var fileEntry in fileEntries)
            {
                bw.BaseStream.Position = _header.dataOffset + fileOffset;
                var writtenSize = fileEntry.WriteFileData(bw.BaseStream, true);

                bw.WriteAlignment(4);

                fileEntry.Entry.fileOffset = fileOffset;
                fileEntry.Entry.fileSize = (uint)writtenSize;

                fileOffset = (uint)(bw.BaseStream.Position - _header.dataOffset);
            }

            bw.BaseStream.Position = HeaderSize_;

            // Write directory entries
            _header.directoryCount = directoryEntries.Count;
            _header.directoryEntriesCount = (short)directoryEntries.Count;
            _header.directoryEntriesOffset = HeaderSize_;

            WriteDirectoryEntries(directoryEntries, bw);

            // Write directory hashes
            _header.directoryHashCount = (short)directoryHashes.Count;
            _header.directoryHashOffset = (int)bw.BaseStream.Position;

            WriteDirectoryHashes(directoryHashes, bw);

            // Write file entry hashes
            _header.fileCount = fileEntries.Count;
            _header.fileEntriesCount = fileEntries.Count;
            _header.fileEntriesOffset = (int)bw.BaseStream.Position;

            WriteFileEntries(fileEntries, bw);

            // Write name table
            _header.nameOffset = (int)bw.BaseStream.Position;
            _header.tableChunkSize = (int)((_header.nameOffset + nameStream.Length + 3) & ~3) - HeaderSize_;

            nameStream.Position = 0;
            nameStream.CopyTo(bw.BaseStream);
            bw.WriteAlignment(4);

            // Write header
            bw.BaseStream.Position = 0;
            WriteHeader(_header, bw);
        }

        private B123ArchiveFile CreateAfi(Stream input, string filePath, B123FileEntry entry)
        {
            ArchiveFileInfo fileInfo;

            input.Position = 0;
            using var br = new BinaryReaderX(input, true);

            if (br.ReadString(4) != "SSZL")
            {
                fileInfo = new ArchiveFileInfo
                {
                    FileData = input,
                    FilePath = filePath,
                    PluginIds = B123Support.RetrievePluginMapping(input, filePath)
                };
                return new B123ArchiveFile(fileInfo, entry);
            }

            br.BaseStream.Position = 0xC;
            int decompressedSize = br.ReadInt32();

            fileInfo = new CompressedArchiveFileInfo
            {
                FileData = input,
                FilePath = filePath,
                Compression = Compressions.Level5.Inazuma3Lzss.Build(),
                DecompressedSize = decompressedSize,
                PluginIds = B123Support.RetrievePluginMapping(input, filePath)
            };
            return new B123ArchiveFile(fileInfo, entry);
        }

        // TODO: Hashes of files to lower?
        private void BuildTables(IEnumerable<B123ArchiveFile> files,
            out IList<B123DirectoryEntry> directoryEntries, out IList<uint> directoryHashes,
            out IList<B123ArchiveFile> fileEntries, out Stream nameStream)
        {
            var groupedFiles = files.OrderBy(x => x.FilePath.GetDirectory())
                .GroupBy(x => x.FilePath.GetDirectory())
                .ToArray();

            var crc32 = Crc32.Crc32B;
            var sjis = Encoding.GetEncoding("SJIS");

            nameStream = new MemoryStream();
            using var nameBw = new BinaryWriterX(nameStream, sjis, true);

            var fileInfos = new List<B123ArchiveFile>();
            directoryEntries = new List<B123DirectoryEntry>();
            directoryHashes = new List<uint>();
            foreach (var fileGroup in groupedFiles)
            {
                var fileIndex = fileInfos.Count;
                var fileGroupEntries = fileGroup.ToArray();

                // Add directory entry first
                var directoryNameOffset = (int)nameBw.BaseStream.Position;
                var directoryName = fileGroup.Key.ToRelative().FullName;
                if (!string.IsNullOrEmpty(directoryName))
                    directoryName += "/";
                nameBw.WriteString(directoryName);

                var hash = crc32.ComputeValue(directoryName.ToLower(), sjis);
                var newDirectoryEntry = new B123DirectoryEntry
                {
                    crc32 = string.IsNullOrEmpty(fileGroup.Key.ToRelative().FullName) ? 0xFFFFFFFF : hash,

                    directoryCount = (short)groupedFiles.Count(gf => fileGroup.Key != gf.Key && gf.Key.IsInDirectory(fileGroup.Key, false)),

                    fileCount = (short)fileGroupEntries.Length,
                    firstFileIndex = (short)fileIndex,

                    directoryNameStartOffset = directoryNameOffset,
                    fileNameStartOffset = (int)nameBw.BaseStream.Position
                };
                if (newDirectoryEntry.crc32 != 0xFFFFFFFF)
                    directoryHashes.Add(newDirectoryEntry.crc32);
                directoryEntries.Add(newDirectoryEntry);

                // Write file names in alphabetic order
                foreach (B123ArchiveFile file in fileGroupEntries)
                {
                    file.Entry.nameOffsetInFolder = (uint)(nameBw.BaseStream.Position - newDirectoryEntry.fileNameStartOffset);
                    file.Entry.crc32 = crc32.ComputeValue(file.FilePath.GetName().ToLower(), sjis);

                    nameBw.WriteString(file.FilePath.GetName());
                }

                // Add file entries in order of ascending hash
                fileInfos.AddRange(fileGroupEntries.OrderBy(x => x.Entry.crc32));
            }

            fileEntries = fileInfos;

            // Order directory entries by hash and set directoryIndex accordingly
            var directoryIndex = 0;
            directoryEntries = directoryEntries.OrderBy(x => x.crc32).Select(x =>
            {
                x.firstDirectoryIndex = directoryIndex;
                directoryIndex += x.directoryCount;
                return x;
            }).ToList();
        }

        private B123Header ReadHeader(BinaryReaderX reader)
        {
            return new B123Header
            {
                magic = reader.ReadString(4),
                directoryEntriesOffset = reader.ReadInt32(),
                directoryHashOffset = reader.ReadInt32(),
                fileEntriesOffset = reader.ReadInt32(),
                nameOffset = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                directoryEntriesCount = reader.ReadInt16(),
                directoryHashCount = reader.ReadInt16(),
                fileEntriesCount = reader.ReadInt32(),
                tableChunkSize = reader.ReadInt32(),
                zero1 = reader.ReadInt32(),
                unk2 = reader.ReadUInt32(),
                unk3 = reader.ReadUInt32(),
                unk4 = reader.ReadUInt32(),
                unk5 = reader.ReadUInt32(),
                directoryCount = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                unk7 = reader.ReadUInt32(),
                zero2 = reader.ReadInt32()
            };
        }

        private B123DirectoryEntry[] ReadDirectoryEntries(BinaryReaderX reader, int count)
        {
            var result = new B123DirectoryEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadDirectoryEntry(reader);

            return result;
        }

        private B123DirectoryEntry ReadDirectoryEntry(BinaryReaderX reader)
        {
            return new B123DirectoryEntry
            {
                crc32 = reader.ReadUInt32(),
                fileCount = reader.ReadInt16(),
                directoryCount = reader.ReadInt16(),
                fileNameStartOffset = reader.ReadInt32(),
                firstFileIndex = reader.ReadInt32(),
                firstDirectoryIndex = reader.ReadInt32(),
                directoryNameStartOffset = reader.ReadInt32()
            };
        }

        private B123FileEntry[] ReadFileEntries(BinaryReaderX reader, int count)
        {
            var result = new B123FileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadFileEntry(reader);

            return result;
        }

        private B123FileEntry ReadFileEntry(BinaryReaderX reader)
        {
            return new B123FileEntry
            {
                crc32 = reader.ReadUInt32(),
                nameOffsetInFolder = reader.ReadUInt32(),
                fileOffset = reader.ReadUInt32(),
                fileSize = reader.ReadUInt32()
            };
        }

        private void WriteHeader(B123Header header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.directoryEntriesOffset);
            writer.Write(header.directoryHashOffset);
            writer.Write(header.fileEntriesOffset);
            writer.Write(header.nameOffset);
            writer.Write(header.dataOffset);
            writer.Write(header.directoryEntriesCount);
            writer.Write(header.directoryHashCount);
            writer.Write(header.fileEntriesCount);
            writer.Write(header.tableChunkSize);
            writer.Write(header.zero1);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
            writer.Write(header.unk4);
            writer.Write(header.unk5);
            writer.Write(header.directoryCount);
            writer.Write(header.fileCount);
            writer.Write(header.unk7);
            writer.Write(header.zero2);
        }

        private void WriteDirectoryEntries(IList<B123DirectoryEntry> entries, BinaryWriterX writer)
        {
            foreach (B123DirectoryEntry entry in entries)
                WriteDirectoryEntry(entry, writer);
        }

        private void WriteDirectoryEntry(B123DirectoryEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.crc32);
            writer.Write(entry.fileCount);
            writer.Write(entry.directoryCount);
            writer.Write(entry.fileNameStartOffset);
            writer.Write(entry.firstFileIndex);
            writer.Write(entry.firstDirectoryIndex);
            writer.Write(entry.directoryNameStartOffset);
        }

        private void WriteDirectoryHashes(IList<uint> entries, BinaryWriterX writer)
        {
            foreach (uint entry in entries)
                writer.Write(entry);
        }

        private void WriteFileEntries(IList<B123ArchiveFile> entries, BinaryWriterX writer)
        {
            foreach (B123ArchiveFile entry in entries)
                WriteFileEntry(entry.Entry, writer);
        }

        private void WriteFileEntry(B123FileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.crc32);
            writer.Write(entry.nameOffsetInFolder);
            writer.Write(entry.fileOffset);
            writer.Write(entry.fileSize);
        }
    }
}
