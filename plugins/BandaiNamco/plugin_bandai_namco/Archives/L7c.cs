using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.DataClasses.FileSystem;
using Konnect.Extensions;
using Kryptography.Checksum.Crc;

namespace plugin_bandai_namco.Archives
{
    class L7c
    {
        private const int Alignment_ = 0x200;

        private static readonly Crc32Namco Crc32Namco = Crc32Namco.Create();

        private L7cHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Define string region
            var stringTableOffset = input.Length - _header.stringTableSize;
            using var stringBr = new BinaryReaderX(new SubStream(input, stringTableOffset, _header.stringTableSize));

            // Read file infos
            input.Position = _header.fileInfoOffset;
            var fileInfos = ReadInfoEntries(br, _header.fileInfoCount);

            // Read file entries
            var fileEntries = ReadEntries(br, _header.fileCount);

            // Read chunks
            var chunks = ReadChunkEntries(br, _header.chunkCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                var file = fileEntries[i];
                var info = fileInfos.First(x => x.id == i);

                var chunkEntries = chunks.Skip(file.chunkIndex).Take(file.chunkCount).ToArray();
                var subStream = new SubStream(input, file.offset, file.compSize);
                var name = (UPath)ReadString(stringBr, info.folderNameOffset) / ReadString(stringBr, info.fileNameOffset);

                result.Add(new L7cArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name.FullName,
                    FileData = subStream
                }, chunkEntries, file));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            var stringStream = new MemoryStream();
            using var stringBr = new BinaryWriterX(stringStream);

            var fileTree = files.ToTree();

            // Write files
            output.Position = Alignment_;
            var fileEntries = WriteFiles(fileTree, output).ToArray();

            // Write file infos
            _header.fileInfoOffset = (int)output.Position;
            var fileInfos = EnumerateFileInfos(fileTree, stringBr).ToArray();
            _header.fileInfoCount = fileInfos.Length;
            WriteInfoEntries(fileInfos, bw);

            // Write file entries
            _header.fileCount = fileEntries.Length;
            WriteEntries(fileEntries, bw);

            // Write chunks
            var chunks = EnumerateChunks(fileTree).ToArray();
            _header.chunkCount = chunks.Length;
            WriteChunkEntries(chunks, bw);

            // Write strings
            stringStream.Position = 0;
            _header.stringTableSize = (int)stringStream.Length;
            stringStream.CopyTo(output);

            // Write header
            _header.fileInfoSize = (int)(output.Length - _header.fileInfoOffset);
            _header.archiveSize = (int)output.Length;
            _header.directoryCount = CountDirectories(fileTree);

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private string ReadString(BinaryReaderX br, long offset)
        {
            br.BaseStream.Position = offset;

            var length = br.ReadByte();
            return br.ReadString(length, Encoding.UTF8);
        }

        private IEnumerable<L7cFileEntry> WriteFiles(DirectoryEntry entry, Stream output, int chunkIndex = 0)
        {
            // Write files
            foreach (var file in entry.Files.Cast<L7cArchiveFile>())
            {
                // Write file data
                file.Entry.offset = (int)output.Position;
                var writtenSize = file.WriteFileData(output);

                // Update file entry
                file.Entry.decompSize = (int)file.FileSize;
                file.Entry.compSize = (int)writtenSize;
                file.Entry.chunkIndex = chunkIndex;
                file.Entry.chunkCount = file.Chunks.Count;

                chunkIndex += file.Entry.chunkCount;
                yield return file.Entry;

                if (output.Position % Alignment_ == 0)
                    output.Position++;
                while (output.Position % Alignment_ != 0)
                    output.WriteByte(0);
            }

            // Write directory contents
            foreach (var dir in entry.Directories)
            {
                foreach (var fileEntry in WriteFiles(dir, output, chunkIndex))
                {
                    chunkIndex += fileEntry.chunkCount;
                    yield return fileEntry;
                }
            }
        }

        private IEnumerable<L7cFileInfoEntry> EnumerateFileInfos(DirectoryEntry entry, BinaryWriterX stringBw, IDictionary<string, int>? stringDict = null, int entryId = 0, int directoryNameOffset = 0)
        {
            stringDict ??= new Dictionary<string, int>();

            // Enumerate files
            foreach (var file in entry.Files)
            {
                int fileNameOffset;
                if (stringDict.ContainsKey(file.FilePath.GetName()))
                    fileNameOffset = stringDict[file.FilePath.GetName()];
                else
                {
                    fileNameOffset = (int)stringBw.BaseStream.Position;
                    stringBw.WriteString(file.FilePath.GetName(), Encoding.UTF8, true, false);
                    stringDict[file.FilePath.GetName()] = fileNameOffset;
                }

                var info = new L7cFileInfoEntry
                {
                    id = entryId++,
                    folderNameOffset = directoryNameOffset,
                    fileNameOffset = fileNameOffset,
                    hash = Crc32Namco.ComputeValue(file.FilePath.ToRelative().FullName.ToLower(), Encoding.UTF8),
                    timestamp = DateTime.Now.ToFileTime()
                };

                yield return info;
            }

            // Enumerate directories
            foreach (var directory in entry.Directories)
            {
                if (stringDict.ContainsKey(directory.AbsolutePath.FullName))
                    directoryNameOffset = stringDict[directory.AbsolutePath.FullName];
                else
                {
                    directoryNameOffset = (int)stringBw.BaseStream.Position;
                    stringBw.WriteString(directory.AbsolutePath.FullName, Encoding.UTF8, true, false);
                    stringDict[directory.AbsolutePath.FullName] = directoryNameOffset;
                }

                yield return new L7cFileInfoEntry
                {
                    id = -1,
                    folderNameOffset = directoryNameOffset,
                    hash = Crc32Namco.ComputeValue(directory.AbsolutePath.FullName.ToLower(), Encoding.UTF8),
                    timestamp = DateTime.Now.ToFileTime()
                };

                foreach (var info in EnumerateFileInfos(directory, stringBw, stringDict, entryId, directoryNameOffset))
                {
                    if (info.IsFile)
                        entryId++;

                    yield return info;
                }
            }
        }

        private IEnumerable<L7cChunkEntry> EnumerateChunks(DirectoryEntry entry)
        {
            // Enumerate through files
            foreach (var file in entry.Files.Cast<L7cArchiveFile>())
            {
                foreach (var chunk in file.Chunks)
                    yield return chunk;
            }

            // Enumerate through directories
            foreach (var dir in entry.Directories)
            {
                foreach (var chunk in EnumerateChunks(dir))
                    yield return chunk;
            }
        }

        private int CountDirectories(DirectoryEntry entry)
        {
            var dirCount = 0;

            // Count directories
            foreach (var dir in entry.Directories)
                dirCount += CountDirectories(dir);

            return dirCount + entry.Directories.Count;
        }

        #region Reading

        private L7cHeader ReadHeader(BinaryReaderX reader)
        {
            return new L7cHeader
            {
                magic = reader.ReadString(4),
                unk = reader.ReadInt32(),
                archiveSize = reader.ReadInt32(),
                fileInfoOffset = reader.ReadInt32(),
                fileInfoSize = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                fileInfoCount = reader.ReadInt32(),
                directoryCount = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                chunkCount = reader.ReadInt32(),
                stringTableSize = reader.ReadInt32(),
                unk4 = reader.ReadInt32()
            };
        }

        private L7cFileInfoEntry[] ReadInfoEntries(BinaryReaderX reader, int count)
        {
            var result = new L7cFileInfoEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadInfoEntry(reader);

            return result;
        }

        private L7cFileInfoEntry ReadInfoEntry(BinaryReaderX reader)
        {
            return new L7cFileInfoEntry
            {
                id = reader.ReadInt32(),
                hash = reader.ReadUInt32(),
                folderNameOffset = reader.ReadInt32(),
                fileNameOffset = reader.ReadInt32(),
                timestamp = reader.ReadInt64()
            };
        }

        private L7cFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new L7cFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private L7cFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new L7cFileEntry
            {
                compSize = reader.ReadInt32(),
                decompSize = reader.ReadInt32(),
                chunkIndex = reader.ReadInt32(),
                chunkCount = reader.ReadInt32(),
                offset = reader.ReadInt32(),
                crc32 = reader.ReadUInt32()
            };
        }

        private L7cChunkEntry[] ReadChunkEntries(BinaryReaderX reader, int count)
        {
            var result = new L7cChunkEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadChunkEntry(reader);

            return result;
        }

        private L7cChunkEntry ReadChunkEntry(BinaryReaderX reader)
        {
            return new L7cChunkEntry
            {
                chunkSize = reader.ReadInt32(),
                unk = reader.ReadUInt16(),
                chunkId = reader.ReadUInt16()
            };
        }

        #endregion

        #region Writing

        private void WriteHeader(L7cHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.unk);
            writer.Write(header.archiveSize);
            writer.Write(header.fileInfoOffset);
            writer.Write(header.fileInfoSize);
            writer.Write(header.unk2);
            writer.Write(header.fileInfoCount);
            writer.Write(header.directoryCount);
            writer.Write(header.fileCount);
            writer.Write(header.chunkCount);
            writer.Write(header.stringTableSize);
            writer.Write(header.unk4);
        }

        private void WriteInfoEntries(L7cFileInfoEntry[] entries, BinaryWriterX writer)
        {
            foreach (L7cFileInfoEntry entry in entries)
                WriteInfoEntry(entry, writer);
        }

        private void WriteInfoEntry(L7cFileInfoEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.id);
            writer.Write(entry.hash);
            writer.Write(entry.folderNameOffset);
            writer.Write(entry.fileNameOffset);
            writer.Write(entry.timestamp);
        }

        private void WriteEntries(L7cFileEntry[] entries, BinaryWriterX writer)
        {
            foreach (L7cFileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(L7cFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.compSize);
            writer.Write(entry.decompSize);
            writer.Write(entry.chunkIndex);
            writer.Write(entry.chunkCount);
            writer.Write(entry.offset);
            writer.Write(entry.crc32);
        }

        private void WriteChunkEntries(L7cChunkEntry[] entries, BinaryWriterX writer)
        {
            foreach (L7cChunkEntry entry in entries)
                WriteChunkEntry(entry, writer);
        }

        private void WriteChunkEntry(L7cChunkEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.chunkSize);
            writer.Write(entry.unk);
            writer.Write(entry.chunkId);
        }

        #endregion
    }
}
