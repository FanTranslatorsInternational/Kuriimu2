using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Extensions;
using Kryptography.Checksum.Crc;
using plugin_level5.Common.Compression;

namespace plugin_level5.N3DS.Archive
{
    // Game: Yo-kai Watch
    public class Arc0
    {
        private const int HeaderSize_ = 72;
        private const int DirectoryEntrySize_ = 20;
        private const int DirectoryHashSize_ = 4;
        private const int FileEntrySize_ = 16;

        private Arc0Header _header;

        public List<Arc0ArchiveFile> Load(Stream input)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var br = new BinaryReaderX(input, Encoding.GetEncoding("Shift-JIS"), true);

            // Read header
            _header = ReadHeader(br);

            // Read directory entries
            var directoryEntriesReader = GetDecompressedTableEntries(input,
                _header.directoryEntriesOffset, _header.directoryHashOffset - _header.directoryEntriesOffset);
            var directoryEntries = ReadDirectoryEntries(directoryEntriesReader, _header.directoryEntriesCount);

            // Read file entry table
            var entriesReader = GetDecompressedTableEntries(input,
                _header.fileEntriesOffset, _header.nameOffset - _header.fileEntriesOffset);
            var entries = ReadEntries(entriesReader, _header.fileEntriesCount);

            // Read nameTable
            var nameComp = new SubStream(input, _header.nameOffset, _header.dataOffset - _header.nameOffset);
            var nameStream = new MemoryStream();
            Level5Compressor.Decompress(nameComp, nameStream);

            // Add Files
            using var nameReader = new BinaryReaderX(nameStream);

            var result = new List<Arc0ArchiveFile>();
            foreach (var directory in directoryEntries)
            {
                nameReader.BaseStream.Position = directory.directoryNameStartOffset;
                var directoryName = nameReader.ReadNullTerminatedString();

                var filesInDirectory = entries.Skip(directory.firstFileIndex).Take(directory.fileCount);
                foreach (var file in filesInDirectory)
                {
                    var fileStream = new SubStream(input, _header.dataOffset + file.fileOffset, file.fileSize);

                    nameReader.BaseStream.Position = directory.fileNameStartOffset + file.nameOffsetInFolder;
                    var fileName = nameReader.ReadNullTerminatedString();

                    var fileInfo = new ArchiveFileInfo
                    {
                        FileData = fileStream,
                        FilePath = directoryName + fileName,
                        PluginIds = Arc0Support.RetrievePluginMapping(fileStream, fileName)
                    };

                    result.Add(new Arc0ArchiveFile(fileInfo, file));
                }
            }

            return result;
        }

        public void Save(Stream output, List<Arc0ArchiveFile> files)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Build directory, file, and name tables
            BuildTables(files, out var directoryEntries, out var directoryHashes, out var fileEntries, out var nameStream);

            // -- Write file --

            using var bw = new BinaryWriterX(output);

            bw.BaseStream.Position = HeaderSize_;

            // Write directory entries
            _header.directoryCount = directoryEntries.Count;
            _header.directoryEntriesCount = (short)directoryEntries.Count;
            _header.directoryEntriesOffset = HeaderSize_;

            var directoryEntriesStream = WriteDirectoryEntries(directoryEntries);
            WriteCompressedStream(bw.BaseStream, directoryEntriesStream);
            bw.WriteAlignment(4);

            // Write directory hashes
            _header.directoryHashCount = (short)directoryHashes.Count;
            _header.directoryHashOffset = (int)bw.BaseStream.Position;

            var directoryHashesStream = WriteDirectoryHashes(directoryHashes);
            WriteCompressedStream(bw.BaseStream, directoryHashesStream);
            bw.WriteAlignment(4);

            // Write file entries
            _header.fileCount = fileEntries.Count;
            _header.fileEntriesCount = fileEntries.Count;
            _header.fileEntriesOffset = (int)bw.BaseStream.Position;

            var fileEntriesStream = WriteFileEntries(fileEntries);
            WriteCompressedStream(bw.BaseStream, fileEntriesStream);
            bw.WriteAlignment(4);

            // Write name table
            _header.nameOffset = (int)bw.BaseStream.Position;

            var nameStreamComp = new MemoryStream();
            Compress(nameStream, nameStreamComp, Level5CompressionMethod.Lz10);
            nameStreamComp.CopyTo(bw.BaseStream);
            bw.WriteAlignment(4);

            // Write file data
            _header.dataOffset = (int)bw.BaseStream.Position;
            foreach (Arc0ArchiveFile archiveFile in fileEntries)
            {
                bw.BaseStream.Position = _header.dataOffset + archiveFile.Entry.fileOffset;
                archiveFile.WriteFileData(bw.BaseStream, false);

                bw.WriteAlignment(4);
            }

            // Write header
            _header.tableChunkSize = (int)(directoryEntries.Count * DirectoryEntrySize_ +
                                    directoryHashes.Count * DirectoryHashSize_ +
                                    fileEntries.Count * FileEntrySize_ +
                                    nameStream.Length + 0x20 + 3) & ~3;

            bw.BaseStream.Position = 0;
            WriteHeader(_header, bw);
        }

        private BinaryReaderX GetDecompressedTableEntries(Stream input, int offset, int length)
        {
            var streamComp = new SubStream(input, offset, length);
            var stream = new MemoryStream();
            Level5Compressor.Decompress(streamComp, stream);

            stream.Position = 0;
            return new BinaryReaderX(stream);
        }

        private void BuildTables(List<Arc0ArchiveFile> files,
            out IList<Arc0DirectoryEntry> directoryEntries, out IList<uint> directoryHashes,
            out IList<Arc0ArchiveFile> fileEntries, out Stream nameStream)
        {
            var groupedFiles = files.OrderBy(x => x.FilePath.GetDirectory())
                .GroupBy(x => x.FilePath.GetDirectory())
                .ToArray();

            var crc32 = Crc32.Crc32B;
            var sjis = Encoding.GetEncoding("Shift-JIS");

            nameStream = new MemoryStream();
            using var nameBw = new BinaryWriterX(nameStream, sjis, true);

            var fileInfos = new List<Arc0ArchiveFile>();
            directoryEntries = new List<Arc0DirectoryEntry>();
            directoryHashes = new List<uint>();
            var fileOffset = 0u;
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

                var hash = crc32.ComputeValue(directoryName, sjis);
                var newDirectoryEntry = new Arc0DirectoryEntry
                {
                    crc32 = string.IsNullOrEmpty(fileGroup.Key.ToRelative().FullName) ? 0xFFFFFFFF : hash,

                    directoryCount = (short)groupedFiles.Count(gf => fileGroup.Key != gf.Key && gf.Key.IsInDirectory(fileGroup.Key, false)),

                    fileCount = (short)fileGroupEntries.Length,
                    firstFileIndex = (ushort)fileIndex,

                    directoryNameStartOffset = directoryNameOffset,
                    fileNameStartOffset = (int)nameBw.BaseStream.Position
                };
                if (newDirectoryEntry.crc32 != 0xFFFFFFFF)
                    directoryHashes.Add(newDirectoryEntry.crc32);
                directoryEntries.Add(newDirectoryEntry);

                // Write file names in alphabetic order
                foreach (Arc0ArchiveFile archiveFile in fileGroupEntries)
                {
                    archiveFile.Entry.nameOffsetInFolder = (uint)(nameBw.BaseStream.Position - newDirectoryEntry.fileNameStartOffset);
                    archiveFile.Entry.crc32 = crc32.ComputeValue(archiveFile.FilePath.GetName(), sjis);
                    archiveFile.Entry.fileOffset = fileOffset;
                    archiveFile.Entry.fileSize = (uint)archiveFile.FileSize;

                    fileOffset = (uint)((fileOffset + archiveFile.FileSize + 3) & ~3);

                    nameBw.WriteString(archiveFile.FilePath.GetName(), sjis);
                }

                // Add file entries in order of ascending hash
                fileInfos.AddRange(fileGroupEntries.OrderBy(x => x.Entry.crc32));
            }

            fileEntries = fileInfos;

            // Order directory entries by hash and set directoryIndex accordingly
            var directoryIndex = 0;
            directoryEntries = directoryEntries.OrderBy(x => x.crc32).Select(x =>
            {
                x.firstDirectoryIndex = (ushort)directoryIndex;
                directoryIndex += x.directoryCount;
                return x;
            }).ToList();
        }

        private void WriteCompressedStream(Stream output, Stream decompressedData)
        {
            var optimalCompressedStream = new MemoryStream();
            Compress(decompressedData, optimalCompressedStream, Level5CompressionMethod.NoCompression);

            for (var i = 1; i < 5; i++)
            {
                var compressedStream = new MemoryStream();
                Compress(decompressedData, compressedStream, (Level5CompressionMethod)i);

                if (compressedStream.Length < optimalCompressedStream.Length)
                    optimalCompressedStream = compressedStream;
            }

            optimalCompressedStream.CopyTo(output);
        }

        private void Compress(Stream input, Stream output, Level5CompressionMethod compressionMethod)
        {
            input.Position = 0;
            output.Position = 0;

            Level5Compressor.Compress(input, output, compressionMethod);

            output.Position = 0;
            input.Position = 0;
        }

        private Arc0Header ReadHeader(BinaryReaderX reader)
        {
            return new Arc0Header
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

        private Arc0DirectoryEntry[] ReadDirectoryEntries(BinaryReaderX reader, int count)
        {
            var result = new Arc0DirectoryEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadDirectoryEntry(reader);

            return result;
        }

        private Arc0DirectoryEntry ReadDirectoryEntry(BinaryReaderX reader)
        {
            return new Arc0DirectoryEntry
            {
                crc32 = reader.ReadUInt32(),
                firstDirectoryIndex = reader.ReadUInt16(),
                directoryCount = reader.ReadInt16(),
                firstFileIndex = reader.ReadUInt16(),
                fileCount = reader.ReadInt16(),
                fileNameStartOffset = reader.ReadInt32(),
                directoryNameStartOffset = reader.ReadInt32()
            };
        }

        private Arc0FileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new Arc0FileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private Arc0FileEntry ReadEntry(BinaryReaderX reader)
        {
            return new Arc0FileEntry
            {
                crc32 = reader.ReadUInt32(),
                nameOffsetInFolder = reader.ReadUInt32(),
                fileOffset = reader.ReadUInt32(),
                fileSize = reader.ReadUInt32()
            };
        }

        private void WriteHeader(Arc0Header header, BinaryWriterX writer)
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

        private Stream WriteDirectoryEntries(IList<Arc0DirectoryEntry> entries)
        {
            var stream = new MemoryStream();
            using var writer = new BinaryWriterX(stream, true);

            foreach (Arc0DirectoryEntry entry in entries)
                WriteDirectoryEntry(entry, writer);

            return stream;
        }

        private void WriteDirectoryEntry(Arc0DirectoryEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.crc32);
            writer.Write(entry.firstDirectoryIndex);
            writer.Write(entry.directoryCount);
            writer.Write(entry.firstFileIndex);
            writer.Write(entry.fileCount);
            writer.Write(entry.fileNameStartOffset);
            writer.Write(entry.directoryNameStartOffset);
        }

        private Stream WriteDirectoryHashes(IList<uint> entries)
        {
            var stream = new MemoryStream();
            using var writer = new BinaryWriterX(stream, true);

            foreach (uint entry in entries)
                writer.Write(entry);

            return stream;
        }

        private Stream WriteFileEntries(IList<Arc0ArchiveFile> entries)
        {
            var stream = new MemoryStream();
            using var writer = new BinaryWriterX(stream, true);

            foreach (Arc0ArchiveFile entry in entries)
                WriteFileEntry(entry.Entry, writer);

            return stream;
        }

        private void WriteFileEntry(Arc0FileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.crc32);
            writer.Write(entry.nameOffsetInFolder);
            writer.Write(entry.fileOffset);
            writer.Write(entry.fileSize);
        }
    }
}
