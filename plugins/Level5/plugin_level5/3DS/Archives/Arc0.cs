using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;
using Kryptography.Hash.Crc;
using plugin_level5.Compression;

namespace plugin_level5._3DS.Archives
{
    // Game: Yo-kai Watch
    class Arc0
    {
        private readonly int _headerSize = Tools.MeasureType(typeof(Arc0Header));
        private readonly int _directoryEntrySize = Tools.MeasureType(typeof(Arc0DirectoryEntry));
        private readonly int _directoryHashSize = Tools.MeasureType(typeof(uint));
        private readonly int _fileEntrySize = Tools.MeasureType(typeof(Arc0FileEntry));

        private Arc0Header _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<Arc0Header>();

            // Read directory entries
            var directoryEntries = ReadCompressedTableEntries<Arc0DirectoryEntry>(input,
                _header.directoryEntriesOffset, _header.directoryHashOffset - _header.directoryEntriesOffset,
                _header.directoryEntriesCount);

            // Read directory hashes
            var directoryHashes = ReadCompressedTableEntries<uint>(input,
                _header.directoryHashOffset, _header.fileEntriesOffset - _header.directoryHashOffset,
                _header.directoryHashCount);

            // Read file entry table
            var entries = ReadCompressedTableEntries<Arc0FileEntry>(input,
                _header.fileEntriesOffset, _header.nameOffset - _header.fileEntriesOffset,
                _header.fileEntriesCount);

            // Read nameTable
            var nameComp = new SubStream(input, _header.nameOffset, _header.dataOffset - _header.nameOffset);
            var nameStream = new MemoryStream();
            Level5Compressor.Decompress(nameComp, nameStream);

            // Add Files
            var names = new BinaryReaderX(nameStream);
            var result = new List<IArchiveFileInfo>();
            foreach (var directory in directoryEntries)
            {
                names.BaseStream.Position = directory.directoryNameStartOffset;
                var directoryName = names.ReadCStringSJIS();

                var filesInDirectory = entries.Skip(directory.firstFileIndex).Take(directory.fileCount);
                foreach (var file in filesInDirectory)
                {
                    var fileStream = new SubStream(input, _header.dataOffset + file.fileOffset, file.fileSize);

                    names.BaseStream.Position = directory.fileNameStartOffset + file.nameOffsetInFolder;
                    var fileName = names.ReadCStringSJIS();

                    result.Add(new Arc0ArchiveFileInfo(fileStream, directoryName + fileName, file)
                    {
                        PluginIds = Arc0Support.RetrievePluginMapping(fileStream, fileName)
                    });
                }
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files, IProgressContext progress)
        {
            // Group files by directory
            var castedFiles = files.Cast<Arc0ArchiveFileInfo>();

            // Build directory, file, and name tables
            BuildTables(castedFiles, out var directoryEntries, out var directoryHashes, out var fileEntries, out var nameStream);

            // -- Write file --

            using var bw = new BinaryWriterX(output);
            bw.BaseStream.Position = _headerSize;

            // Write directory entries
            _header.directoryCount = directoryEntries.Count;
            _header.directoryEntriesCount = (short)directoryEntries.Count;
            _header.directoryEntriesOffset = _headerSize;

            WriteCompressedTableEntries(bw.BaseStream, directoryEntries);
            bw.WriteAlignment(4);

            // Write directory hashes
            _header.directoryHashCount = (short)directoryHashes.Count;
            _header.directoryHashOffset = (int)bw.BaseStream.Position;

            WriteCompressedTableEntries(bw.BaseStream, directoryHashes);
            bw.WriteAlignment(4);

            // Write file entries
            _header.fileCount = fileEntries.Count;
            _header.fileEntriesCount = fileEntries.Count;
            _header.fileEntriesOffset = (int)bw.BaseStream.Position;

            WriteCompressedTableEntries(bw.BaseStream, fileEntries.Select(x => x.Entry));
            bw.WriteAlignment(4);

            // Write name table
            _header.nameOffset = (int)bw.BaseStream.Position;

            var nameStreamComp = new MemoryStream();
            Compress(nameStream, nameStreamComp, Level5CompressionMethod.Lz10);
            nameStreamComp.CopyTo(bw.BaseStream);
            bw.WriteAlignment(4);

            // Write file data
            _header.dataOffset = (int)bw.BaseStream.Position;
            foreach (var fileEntry in fileEntries)
            {
                bw.BaseStream.Position = _header.dataOffset + fileEntry.Entry.fileOffset;
                fileEntry.SaveFileData(bw.BaseStream);
            }

            // Write header
            _header.tableChunkSize = (int)(directoryEntries.Count * _directoryEntrySize +
                                    directoryHashes.Count * _directoryHashSize +
                                    fileEntries.Count * _fileEntrySize +
                                    nameStream.Length + 0x20 + 3) & ~3;

            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private IList<TTable> ReadCompressedTableEntries<TTable>(Stream input, int offset, int length, int count)
        {
            var streamComp = new SubStream(input, offset, length);
            var stream = new MemoryStream();
            Level5Compressor.Decompress(streamComp, stream);

            stream.Position = 0;
            return new BinaryReaderX(stream).ReadMultiple<TTable>(count);
        }

        private void BuildTables(IEnumerable<Arc0ArchiveFileInfo> files,
            out IList<Arc0DirectoryEntry> directoryEntries, out IList<uint> directoryHashes,
            out IList<Arc0ArchiveFileInfo> fileEntries, out Stream nameStream)
        {
            var groupedFiles = files.OrderBy(x => x.FilePath.GetDirectory())
                .GroupBy(x => x.FilePath.GetDirectory())
                .ToArray();

            var crc32 = Crc32.Default;
            var sjis = Encoding.GetEncoding("SJIS");

            nameStream = new MemoryStream();
            using var nameBw = new BinaryWriterX(nameStream, true);

            var fileInfos = new List<Arc0ArchiveFileInfo>();
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
                nameBw.WriteString(directoryName, sjis, false);

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
                foreach (var fileEntry in fileGroupEntries)
                {
                    fileEntry.Entry.nameOffsetInFolder = (uint)(nameBw.BaseStream.Position - newDirectoryEntry.fileNameStartOffset);
                    fileEntry.Entry.crc32 = crc32.ComputeValue(fileEntry.FilePath.GetName(), sjis);
                    fileEntry.Entry.fileOffset = fileOffset;
                    fileEntry.Entry.fileSize = (uint)fileEntry.FileSize;

                    fileOffset = (uint)((fileOffset + fileEntry.FileSize + 3) & ~3);

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
                x.firstDirectoryIndex = (ushort)directoryIndex;
                directoryIndex += x.directoryCount;
                return x;
            }).ToList();
        }

        private void WriteCompressedTableEntries<TTable>(Stream output, IEnumerable<TTable> table)
        {
            var decompressedStream = new MemoryStream();
            using var decompressedBw = new BinaryWriterX(decompressedStream, true);
            decompressedBw.WriteMultiple(table);

            var optimalCompressedStream = new MemoryStream();
            Compress(decompressedStream, optimalCompressedStream, Level5CompressionMethod.NoCompression);

            for (var i = 1; i < 5; i++)
            {
                var compressedStream = new MemoryStream();
                Compress(decompressedStream, compressedStream, (Level5CompressionMethod)i);

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
    }
}
