using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.Extensions;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kryptography.Hash.Crc;

namespace plugin_bandai_namco.Archives
{
    class L7c
    {
        private static readonly Crc32Namco Crc32Namco = Crc32Namco.Create();

        private const int Alignment_ = 0x200;

        private L7cHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<L7cHeader>();

            // Define string region
            var stringTableOffset = input.Length - _header.stringTableSize;
            using var stringBr = new BinaryReaderX(new SubStream(input, stringTableOffset, _header.stringTableSize));

            // Read file infos
            input.Position = _header.fileInfoOffset;
            var fileInfos = br.ReadMultiple<L7cFileInfoEntry>(_header.fileInfoCount);

            // Read file entries
            var fileEntries = br.ReadMultiple<L7cFileEntry>(_header.fileCount);

            // Read chunks
            var chunks = br.ReadMultiple<L7cChunkEntry>(_header.chunkCount);

            // Add files
            var lowerParts = new List<int>();

            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                var file = fileEntries[i];
                var info = fileInfos.First(x => x.id == i);

                lowerParts.Add(file.offset & 0x1FFF);

                var chunkEntries = chunks.Skip(file.chunkIndex).Take(file.chunkCount).ToArray();
                var subStream = new SubStream(input, file.offset, file.compSize);
                var name = (UPath)ReadString(stringBr, info.folderNameOffset) / ReadString(stringBr, info.fileNameOffset);

                result.Add(new L7cArchiveFileInfo(subStream, name.FullName, chunkEntries, file));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
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
            bw.WriteMultiple(fileInfos);

            // Write file entries
            _header.fileCount = fileEntries.Length;
            bw.WriteMultiple(fileEntries);

            // Write chunks
            var chunks = EnumerateChunks(fileTree).ToArray();
            _header.chunkCount = chunks.Length;
            bw.WriteMultiple(chunks);

            // Write strings
            stringStream.Position = 0;
            _header.stringTableSize = (int)stringStream.Length;
            stringStream.CopyTo(output);

            // Write header
            _header.fileInfoSize = (int)(output.Length - _header.fileInfoOffset);
            _header.archiveSize = (int)output.Length;
            _header.directoryCount = CountDirectories(fileTree);

            output.Position = 0;
            bw.WriteType(_header);
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
            foreach (var file in entry.Files.Cast<L7cArchiveFileInfo>())
            {
                file.SaveFileData(output);

                file.Entry.chunkIndex = chunkIndex;
                chunkIndex += file.Entry.chunkCount;
                yield return file.Entry;

                if (output.Position % Alignment_ == 0)
                    output.Position++;
                while (output.Position % Alignment_ != 0)
                    output.WriteByte(0);
            }

            // Write directory contents first
            foreach (var dir in entry.Directories)
            {
                foreach (var fileEntry in WriteFiles(dir, output, chunkIndex))
                {
                    chunkIndex += fileEntry.chunkCount;
                    yield return fileEntry;
                }
            }
        }

        private IEnumerable<L7cFileInfoEntry> EnumerateFileInfos(DirectoryEntry entry, BinaryWriterX stringBw, IDictionary<string, int> stringDict = null, int entryId = 0, int directoryNameOffset = 0)
        {
            if (stringDict == null)
                stringDict = new Dictionary<string, int>();

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
                    hash = BinaryPrimitives.ReadUInt32BigEndian(Crc32Namco.Compute(Encoding.UTF8.GetBytes(file.FilePath.ToRelative().FullName.ToLower()))),
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
                    hash = BinaryPrimitives.ReadUInt32BigEndian(Crc32Namco.Compute(Encoding.UTF8.GetBytes(directory.AbsolutePath.FullName.ToLower()))),
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
            foreach (var file in entry.Files.Cast<L7cArchiveFileInfo>())
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
    }
}
