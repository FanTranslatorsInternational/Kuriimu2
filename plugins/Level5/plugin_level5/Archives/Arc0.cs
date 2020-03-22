using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;
using Kryptography.Hash.Crc;
using plugin_level5.Compression;

namespace plugin_level5.Archives
{
    class Arc0
    {
        private readonly int _headerSize = Tools.MeasureType(typeof(Arc0Header));

        private Arc0Header _header;

        private IList<Arc0DirectoryEntry> _directoryEntries;
        private IList<uint> _directoryHashes;

        private IList<Arc0FileEntry> _entries;

        private Stream _nameComp;

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<Arc0Header>();

            // Read directory entries
            _directoryEntries = ReadCompressedTableEntries<Arc0DirectoryEntry>(input,
                _header.directoryEntriesOffset, _header.directoryHashOffset - _header.directoryEntriesOffset,
                _header.directoryEntriesCount);

            // Read directory hashes
            _directoryHashes = ReadCompressedTableEntries<uint>(input,
                _header.directoryHashOffset, _header.fileEntriesOffset - _header.directoryHashOffset,
                _header.directoryHashCount);

            // Read file entry table
            _entries = ReadCompressedTableEntries<Arc0FileEntry>(input,
                _header.fileEntriesOffset, _header.nameOffset - _header.fileEntriesOffset,
                _header.fileEntriesCount);

            // Read nameTable
            _nameComp = new SubStream(input, _header.nameOffset, _header.dataOffset - _header.nameOffset);
            var nameStream = new MemoryStream();
            Compressor.Decompress(_nameComp, nameStream);

            // Add Files
            var names = new BinaryReaderX(nameStream);
            var result = new List<ArchiveFileInfo>();
            foreach (var directory in _directoryEntries)
            {
                var filesInDirectory = _entries.Skip(directory.firstFileIndex).Take(directory.fileCount);
                foreach (var file in filesInDirectory)
                {
                    var fileStream = new SubStream(input, _header.dataOffset + file.fileOffset, file.fileSize);

                    names.BaseStream.Position = directory.fileNameStartOffset + file.nameOffsetInFolder;
                    var fileName = names.ReadCStringSJIS();
                    names.BaseStream.Position = directory.directoryNameStartOffset;
                    var directoryName = names.ReadCStringSJIS();

                    result.Add(new Arc0ArchiveFileInfo(fileStream, directoryName + fileName, file)
                    {
                        PluginIds = Arc0Support.RetrievePluginMapping(fileStream, fileName)
                    });
                }
            }

            return result;
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files, IProgressContext progress)
        {
            // Group files by directory
            var castedFiles = files.Cast<Arc0ArchiveFileInfo>();

            // Build directory, file, and name tables
            BuildTables(castedFiles, out var directoryEntries, out var directoryHashes, out var fileEntries, out var nameStream);

            // -- Write file --

            using var bw = new BinaryWriterX(output);
            bw.BaseStream.Position = _headerSize;

            // Write directory entries
            _header.directoryCount = (uint)directoryEntries.Count;
            _header.directoryEntriesCount = (short)directoryEntries.Count;
            _header.directoryEntriesOffset = _headerSize;

            WriteCompressedTableEntries(bw.BaseStream, directoryEntries);
            bw.WriteAlignment(4);

            // Write directory hashes
            _header.directoryHashCount = (short)directoryHashes.Count;
            _header.directoryHashOffset = (int)bw.BaseStream.Position;

            WriteCompressedTableEntries(bw.BaseStream, directoryHashes);
            bw.WriteAlignment(4);

            // Write file entry hashes
            _header.fileCount = fileEntries.Count;
            _header.fileEntriesCount = (short)fileEntries.Count;
            _header.fileEntriesOffset = (int)bw.BaseStream.Position;

            WriteCompressedTableEntries(bw.BaseStream, fileEntries.Select(x => x.Entry));
            bw.WriteAlignment(4);

            // Write name table
            _header.nameOffset = (int)bw.BaseStream.Position;

            var nameStreamComp = new MemoryStream();
            Compress(nameStream, nameStreamComp, CompressionMethod.Lz10);
            nameStreamComp.CopyTo(bw.BaseStream);
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

        private IList<TTable> ReadCompressedTableEntries<TTable>(Stream input, int offset, int length, int count)
        {
            var streamComp = new SubStream(input, offset, length);
            var stream = new MemoryStream();
            Compressor.Decompress(streamComp, stream);

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

            var crc32 = Crc32.Create(Crc32Formula.Normal);
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

                var hash = ToUInt32BigEndian(crc32.Compute(sjis.GetBytes(directoryName)));
                var newDirectoryEntry = new Arc0DirectoryEntry
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
                    fileEntry.Entry.crc32 = ToUInt32BigEndian(crc32.Compute(sjis.GetBytes(fileEntry.FilePath.GetName())));
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
                x.firstDirectoryIndex = (short)directoryIndex;
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
            Compress(decompressedStream, optimalCompressedStream, CompressionMethod.NoCompression);

            for (var i = 1; i < 5; i++)
            {
                var compressedStream = new MemoryStream();
                Compress(decompressedStream, compressedStream, (CompressionMethod)i);

                if (compressedStream.Length < optimalCompressedStream.Length)
                    optimalCompressedStream = compressedStream;
            }

            optimalCompressedStream.CopyTo(output);
        }

        private void Compress(Stream input, Stream output, CompressionMethod compressionMethod)
        {
            input.Position = 0;
            output.Position = 0;

            Compressor.Compress(input, output, compressionMethod);

            output.Position = 0;
            input.Position = 0;
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
