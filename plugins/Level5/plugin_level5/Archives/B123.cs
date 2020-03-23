using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kryptography.Hash.Crc;
using plugin_level5.Compression;

namespace plugin_level5.Archives
{
    // Game: Inazuma 3 Ogre Team
    class B123
    {
        private readonly int _headerSize = Tools.MeasureType(typeof(B123Header));
        private B123Header _header;

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
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
            var result = new List<ArchiveFileInfo>();
            foreach (var directory in directoryEntries)
            {
                var filesInDirectory = entries.Skip(directory.firstFileIndex).Take(directory.fileCount);
                foreach (var file in filesInDirectory)
                {
                    var fileStream = new SubStream(input, _header.dataOffset + file.fileOffset, file.fileSize);

                    br.BaseStream.Position =_header.nameOffset + 
                                            directory.fileNameStartOffset + 
                                            file.nameOffsetInFolder;
                    var fileName = br.ReadCStringSJIS();

                    br.BaseStream.Position = _header.nameOffset +
                                             directory.directoryNameStartOffset;
                    var directoryName = br.ReadCStringSJIS();

                    result.Add(new B123ArchiveFileInfo(fileStream, directoryName + fileName, file)
                    {
                        PluginIds = B123Support.RetrievePluginMapping(fileStream, fileName)
                    });
                }
            }

            return result;
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
            // Group files by directory
            var castedFiles = files.Cast<B123ArchiveFileInfo>();

            // Build directory, file, and name tables
            BuildTables(castedFiles, out var directoryEntries, out var directoryHashes, out var fileEntries, out var nameStream);

            // -- Write file --

            using var bw = new BinaryWriterX(output);
            bw.BaseStream.Position = _headerSize;

            // Write directory entries
            _header.directoryCount = (uint)directoryEntries.Count;
            _header.directoryEntriesCount = (short)directoryEntries.Count;
            _header.directoryEntriesOffset = _headerSize;

            bw.WriteMultiple(directoryEntries);
            bw.WriteAlignment(4);

            // Write directory hashes
            _header.directoryHashCount = (short)directoryHashes.Count;
            _header.directoryHashOffset = (int)bw.BaseStream.Position;

            bw.WriteMultiple(directoryHashes);
            bw.WriteAlignment(4);

            // Write file entry hashes
            _header.fileCount = fileEntries.Count;
            _header.fileEntriesCount = (short)fileEntries.Count;
            _header.fileEntriesOffset = (int)bw.BaseStream.Position;

            bw.WriteMultiple(fileEntries.Select(x => x.Entry));
            bw.WriteAlignment(4);

            // Write name table
            _header.nameOffset = (int)bw.BaseStream.Position;

            nameStream.Position = 0;
            nameStream.CopyTo(bw.BaseStream);
            bw.WriteAlignment(4);

            // Write file data
            _header.dataOffset = (int)bw.BaseStream.Position;

            foreach (var fileEntry in fileEntries)
            {
                bw.BaseStream.Position = _header.dataOffset + fileEntry.Entry.fileOffset;
                fileEntry.SaveFileData(bw.BaseStream, null);
            }

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private void BuildTables(IEnumerable<B123ArchiveFileInfo> files,
            out IList<B123DirectoryEntry> directoryEntries, out IList<uint> directoryHashes,
            out IList<B123ArchiveFileInfo> fileEntries, out Stream nameStream)
        {
            var groupedFiles = files.OrderBy(x => x.FilePath.GetDirectory())
                .GroupBy(x => x.FilePath.GetDirectory())
                .ToArray();

            var crc32 = Crc32.Create(Crc32Formula.Normal);
            var sjis = Encoding.GetEncoding("SJIS");

            nameStream = new MemoryStream();
            using var nameBw = new BinaryWriterX(nameStream, true);

            var fileInfos = new List<B123ArchiveFileInfo>();
            directoryEntries = new List<B123DirectoryEntry>();
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
                nameBw.WriteString(directoryName, sjis, false);

                var hash = ToUInt32BigEndian(crc32.Compute(sjis.GetBytes(directoryName.ToLower())));
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
                    fileEntry.Entry.crc32 = ToUInt32BigEndian(crc32.Compute(sjis.GetBytes(fileEntry.FilePath.GetName().ToLower())));
                    fileEntry.Entry.fileOffset = fileOffset;
                    fileEntry.Entry.fileSize = (uint)fileEntry.FileSize;

                    fileOffset = (uint)((fileOffset + fileEntry.FileSize + 3) & ~3);

                    nameBw.WriteString(fileEntry.FilePath.GetName(), sjis, false);
                }

                // Add file entries in order of ascending crc32
                fileInfos.AddRange(fileGroupEntries.OrderBy(x => x.Entry.crc32));
            }

            fileEntries = fileInfos;

            // Order directory entries by crc32 and set directoryIndex accordingly
            var directoryIndex = 0;
            directoryEntries = directoryEntries.OrderBy(x => x.crc32).Select(x =>
            {
                x.firstDirectoryIndex = directoryIndex;
                directoryIndex += x.directoryCount;
                return x;
            }).ToList();
        }

        // TODO: Remove when only net core
        private uint ToUInt32BigEndian(byte[] input)
        {
#if NET_CORE_31
            return BinaryPrimitives.ReadUInt32BigEndian(input);
#else
            return (uint)((input[0] << 24) | (input[1] << 16) | (input[2] << 8) | input[3]);
#endif
        }
    }
}
