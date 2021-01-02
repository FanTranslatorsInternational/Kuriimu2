using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Extensions;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;
using Kryptography.Hash.Crc;

namespace plugin_level5._3DS.Archives
{
    // Game: Inazuma 3 Ogre Team
    class B123
    {
        private readonly int _headerSize = Tools.MeasureType(typeof(B123Header));
        private readonly int _directoryEntrySize = Tools.MeasureType(typeof(B123DirectoryEntry));
        private readonly int _directoryHashSize = Tools.MeasureType(typeof(uint));
        private readonly int _fileEntrySize = Tools.MeasureType(typeof(B123FileEntry));

        private B123Header _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<B123Header>();

            // Read directory entries
            br.BaseStream.Position = _header.directoryEntriesOffset;
            var directoryEntries = br.ReadMultiple<B123DirectoryEntry>(_header.directoryEntriesCount);

            // Read directory hashes
            br.BaseStream.Position = _header.directoryHashOffset;
            var directoryHashes = br.ReadMultiple<uint>(_header.directoryHashCount);

            // Read file entry table
            br.BaseStream.Position = _header.fileEntriesOffset;
            var entries = br.ReadMultiple<B123FileEntry>(_header.fileEntriesCount);

            // Add Files
            var result = new List<IArchiveFileInfo>();
            foreach (var directory in directoryEntries)
            {
                var filesInDirectory = entries.Skip(directory.firstFileIndex).Take(directory.fileCount);
                foreach (var file in filesInDirectory)
                {
                    var fileStream = new SubStream(input, _header.dataOffset + file.fileOffset, file.fileSize);

                    br.BaseStream.Position = _header.nameOffset +
                                            directory.fileNameStartOffset +
                                            file.nameOffsetInFolder;
                    var fileName = br.ReadCStringSJIS();

                    br.BaseStream.Position = _header.nameOffset +
                                             directory.directoryNameStartOffset;
                    var directoryName = br.ReadCStringSJIS();

                    result.Add(CreateAfi(fileStream, directoryName + fileName, file));
                }
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files, IProgressContext progress)
        {
            // Prepare progressbar
            var splittedProgress = progress.SplitIntoEvenScopes(2);

            // Group files by directory
            var castedFiles = files.Cast<B123ArchiveFileInfo>();

            // Build directory, file, and name tables
            BuildTables(castedFiles, out var directoryEntries, out var directoryHashes, out var fileEntries, out var nameStream);

            // -- Write file --

            using var bw = new BinaryWriterX(output);
            bw.BaseStream.Position = _headerSize;

            _header.dataOffset = (int)(_headerSize +
                                  directoryEntries.Count * _directoryEntrySize +
                                  directoryHashes.Count * _directoryHashSize +
                                  fileEntries.Count * _fileEntrySize +
                                  nameStream.Length + 3) & ~3;

            // Write file data
            var fileOffset = 0u;
            var fileIndex = 1;
            foreach (var fileEntry in fileEntries)
            {
                splittedProgress[0].ReportProgress($"Write file data {fileIndex}/{fileEntries.Count}", fileIndex++, fileEntries.Count);

                bw.BaseStream.Position = _header.dataOffset + fileOffset;
                var writtenSize = fileEntry.SaveFileData(bw.BaseStream, null);

                fileEntry.Entry.fileOffset = fileOffset;
                fileEntry.Entry.fileSize = (uint)writtenSize;

                fileOffset = (uint)(bw.BaseStream.Position - _header.dataOffset);
            }

            bw.BaseStream.Position = _headerSize;

            // Write directory entries
            _header.directoryCount = directoryEntries.Count;
            _header.directoryEntriesCount = (short)directoryEntries.Count;
            _header.directoryEntriesOffset = _headerSize;

            splittedProgress[1].ReportProgress("Write directory entries", 0, 4);
            bw.WriteMultiple(directoryEntries);

            // Write directory hashes
            _header.directoryHashCount = (short)directoryHashes.Count;
            _header.directoryHashOffset = (int)bw.BaseStream.Position;

            splittedProgress[1].ReportProgress("Write directory hashes", 1, 4);
            bw.WriteMultiple(directoryHashes);

            // Write file entry hashes
            _header.fileCount = fileEntries.Count;
            _header.fileEntriesCount = fileEntries.Count;
            _header.fileEntriesOffset = (int)bw.BaseStream.Position;

            splittedProgress[1].ReportProgress("Write file entries", 2, 4);
            bw.WriteMultiple(fileEntries.Select(x => x.Entry));

            // Write name table
            _header.nameOffset = (int)bw.BaseStream.Position;
            _header.tableChunkSize = (int)((_header.nameOffset + nameStream.Length + 3) & ~3) - _headerSize;

            splittedProgress[1].ReportProgress("Write file & directory names", 3, 4);
            nameStream.Position = 0;
            nameStream.CopyTo(bw.BaseStream);
            bw.WriteAlignment(4);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);

            splittedProgress[1].ReportProgress("Done", 4, 4);
        }

        private ArchiveFileInfo CreateAfi(Stream input, string filePath, B123FileEntry entry)
        {
            input.Position = 0;
            using var br = new BinaryReaderX(input, true);

            if (br.ReadString(4) == "SSZL")
            {
                br.BaseStream.Position = 0xC;
                var decompressedSize = br.ReadInt32();

                return new B123ArchiveFileInfo(input, filePath,
                    Compressions.Level5.Inazuma3Lzss, decompressedSize,
                    entry)
                {
                    PluginIds = B123Support.RetrievePluginMapping(input, filePath)
                };
            }

            return new B123ArchiveFileInfo(input, filePath, entry)
            {
                PluginIds = B123Support.RetrievePluginMapping(input, filePath)
            };
        }

        // TODO: Hashes of files to lower?
        private void BuildTables(IEnumerable<B123ArchiveFileInfo> files,
            out IList<B123DirectoryEntry> directoryEntries, out IList<uint> directoryHashes,
            out IList<B123ArchiveFileInfo> fileEntries, out Stream nameStream)
        {
            var groupedFiles = files.OrderBy(x => x.FilePath.GetDirectory())
                .GroupBy(x => x.FilePath.GetDirectory())
                .ToArray();

            var crc32 = Crc32.Default;
            var sjis = Encoding.GetEncoding("SJIS");

            nameStream = new MemoryStream();
            using var nameBw = new BinaryWriterX(nameStream, true);

            var fileInfos = new List<B123ArchiveFileInfo>();
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
                nameBw.WriteString(directoryName, sjis, false);

                var hash = BinaryPrimitives.ReadUInt32BigEndian(crc32.Compute(sjis.GetBytes(directoryName.ToLower())));
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
                foreach (var fileEntry in fileGroupEntries)
                {
                    fileEntry.Entry.nameOffsetInFolder = (uint)(nameBw.BaseStream.Position - newDirectoryEntry.fileNameStartOffset);
                    fileEntry.Entry.crc32 = BinaryPrimitives.ReadUInt32BigEndian(crc32.Compute(sjis.GetBytes(fileEntry.FilePath.GetName().ToLower())));

                    nameBw.WriteString(fileEntry.FilePath.GetName(), sjis, false);
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
    }
}
