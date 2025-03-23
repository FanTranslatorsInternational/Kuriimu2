using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;
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
            var typeReader = new BinaryTypeReader();

            // Read header
            var header = typeReader.Read<Arc0Header>(br);
            if (header == null)
                return new List<Arc0ArchiveFile>();

            _header = header;

            // Read directory entries
            var directoryEntries = ReadCompressedTableEntries<Arc0DirectoryEntry>(input,
                _header.directoryEntriesOffset, _header.directoryHashOffset - _header.directoryEntriesOffset,
                _header.directoryEntriesCount);

            // Read file entry table
            var entries = ReadCompressedTableEntries<Arc0FileEntry>(input,
                _header.fileEntriesOffset, _header.nameOffset - _header.fileEntriesOffset,
                _header.fileEntriesCount);

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

            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            bw.BaseStream.Position = HeaderSize_;

            // Write directory entries
            _header.directoryCount = directoryEntries.Count;
            _header.directoryEntriesCount = (short)directoryEntries.Count;
            _header.directoryEntriesOffset = HeaderSize_;

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

            WriteCompressedTableEntries(bw.BaseStream, fileEntries);
            bw.WriteAlignment(4);

            // Write name table
            _header.nameOffset = (int)bw.BaseStream.Position;

            var nameStreamComp = new MemoryStream();
            Compress(nameStream, nameStreamComp, Level5CompressionMethod.Lz10);
            nameStreamComp.CopyTo(bw.BaseStream);
            bw.WriteAlignment(4);

            // Write file data
            _header.dataOffset = (int)bw.BaseStream.Position;
            foreach (ArchiveFile archiveFile in fileEntries)
            {
                var fileEntry = (Arc0ArchiveFile)archiveFile;

                bw.BaseStream.Position = _header.dataOffset + fileEntry.Entry.fileOffset;
                fileEntry.WriteFileData(bw.BaseStream, false, null);

                bw.WriteAlignment(4);
            }

            // Write header
            _header.tableChunkSize = (int)(directoryEntries.Count * DirectoryEntrySize_ +
                                    directoryHashes.Count * DirectoryHashSize_ +
                                    fileEntries.Count * FileEntrySize_ +
                                    nameStream.Length + 0x20 + 3) & ~3;

            bw.BaseStream.Position = 0;
            typeWriter.Write(_header, bw);
        }

        private IList<TTable?> ReadCompressedTableEntries<TTable>(Stream input, int offset, int length, int count)
        {
            var typeReader = new BinaryTypeReader();

            var streamComp = new SubStream(input, offset, length);
            var stream = new MemoryStream();
            Level5Compressor.Decompress(streamComp, stream);

            stream.Position = 0;
            using var br = new BinaryReaderX(stream);

            return typeReader.ReadMany<TTable>(br, count);
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

        private void WriteCompressedTableEntries<TTable>(Stream output, IEnumerable<TTable> table)
        {
            var decompressedStream = new MemoryStream();
            using var decompressedBw = new BinaryWriterX(decompressedStream, true);

            var typeWriter = new BinaryTypeWriter();
            typeWriter.WriteMany(table, decompressedBw);

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
