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
    class XFSA2 : IXfsa
    {
        private const int HeaderSize_ = 36;

        private XfsaHeader _header;

        public List<ArchiveFile> Load(Stream input)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var br = new BinaryReaderX(input, Encoding.GetEncoding("Shift-JIS"), true);

            // Header
            _header = ReadHeader(br);

            // Read directory entries
            var directoryEntriesReader = XfsaSupport.GetDecompressedTableEntries(input,
                _header.directoryEntriesOffset, _header.directoryHashOffset - _header.directoryEntriesOffset);
            var directoryEntries = ReadDirectoryEntries(directoryEntriesReader, _header.directoryEntriesCount);

            // Read directory hashes
            var hashEntriesReader = XfsaSupport.GetDecompressedTableEntries(input,
                _header.directoryHashOffset, _header.fileEntriesOffset - _header.directoryHashOffset);
            _ = ReadDirectoryHashes(hashEntriesReader, _header.directoryHashCount);

            // Read file entry table
            var entriesReader = XfsaSupport.GetDecompressedTableEntries(input,
                _header.fileEntriesOffset, _header.nameOffset - _header.fileEntriesOffset);
            var entries = ReadFileEntries(entriesReader, _header.fileEntriesCount);

            // Read nameTable
            var nameComp = new SubStream(input, _header.nameOffset, _header.dataOffset - _header.nameOffset);
            var nameStream = new MemoryStream();
            Level5Compressor.Decompress(nameComp, nameStream);

            // Add Files
            var names = new BinaryReaderX(nameStream);
            var result = new List<ArchiveFile>();
            foreach (var directory in directoryEntries)
            {
                names.BaseStream.Position = directory.directoryNameOffset;
                var directoryName = names.ReadNullTerminatedString();

                var filesInDirectory = entries.Skip(directory.FirstFileIndex).Take(directory.fileCount);
                foreach (var file in filesInDirectory)
                {
                    var fileStream = new SubStream(input, _header.dataOffset + file.FileOffset, file.FileSize);

                    names.BaseStream.Position = directory.fileNameStartOffset + file.NameOffset;
                    var fileName = names.ReadNullTerminatedString();

                    var fileInfo = new ArchiveFileInfo
                    {
                        FileData = fileStream,
                        FilePath = directoryName + fileName,
                        PluginIds = XfsaSupport.RetrievePluginMapping(fileStream, fileName)
                    };

                    result.Add(new XfsaArchiveFile<Xfsa2FileEntry>(fileInfo, file));
                }
            }

            return result;
        }

        public void Save(Stream output, List<ArchiveFile> files)
        {
            // Build directory, file, and name tables
            BuildTables(files, out var directoryEntries, out var directoryHashes, out var fileEntries, out var nameStream);

            // -- Write file --

            using var bw = new BinaryWriterX(output);
            bw.BaseStream.Position = HeaderSize_;

            // Write directory entries
            _header.directoryEntriesCount = (short)directoryEntries.Count;
            _header.directoryEntriesOffset = HeaderSize_;

            var directoryEntriesStream = WriteDirectoryEntries(directoryEntries);
            XfsaSupport.WriteCompressedStream(bw.BaseStream, directoryEntriesStream);
            bw.WriteAlignment(4);

            // Write directory hashes
            _header.directoryHashCount = (short)directoryHashes.Count;
            _header.directoryHashOffset = (int)bw.BaseStream.Position;

            var directoryHashesStream = WriteDirectoryHashes(directoryHashes);
            XfsaSupport.WriteCompressedStream(bw.BaseStream, directoryHashesStream);
            bw.WriteAlignment(4);

            // Write file entry hashes
            _header.fileEntriesCount = fileEntries.Count;
            _header.fileEntriesOffset = (int)bw.BaseStream.Position;

            var fileEntriesStream = WriteFileEntries(fileEntries);
            XfsaSupport.WriteCompressedStream(bw.BaseStream, fileEntriesStream);
            bw.WriteAlignment(4);

            // Write name table
            _header.nameOffset = (int)bw.BaseStream.Position;

            var nameStreamComp = new MemoryStream();
            XfsaSupport.Compress(nameStream, nameStreamComp, Level5CompressionMethod.Lz10);
            nameStreamComp.CopyTo(bw.BaseStream);
            bw.WriteAlignment(4);

            // Write file data
            _header.dataOffset = (int)bw.BaseStream.Position;

            foreach (var file in fileEntries)
            {
                var fileEntry = (XfsaArchiveFile<Xfsa2FileEntry>)file;

                bw.BaseStream.Position = _header.dataOffset + fileEntry.Entry.FileOffset;
                fileEntry.WriteFileData(bw.BaseStream, false);

                bw.WriteAlignment(16);
            }

            // Write header
            bw.BaseStream.Position = 0;
            WriteHeader(_header, bw);
        }

        private void BuildTables(IEnumerable<ArchiveFile> files,
            out IList<Xfsa2DirectoryEntry> directoryEntries, out IList<uint> directoryHashes,
            out IList<XfsaArchiveFile<Xfsa2FileEntry>> fileEntries, out Stream nameStream)
        {
            var groupedFiles = files.OrderBy(x => x.FilePath.GetDirectory())
                .GroupBy(x => x.FilePath.GetDirectory())
                .ToArray();

            var crc32 = Crc32.Crc32B;
            var sjis = Encoding.GetEncoding("SJIS");

            nameStream = new MemoryStream();
            using var nameBw = new BinaryWriterX(nameStream, true);

            var fileInfos = new List<ArchiveFile>();
            directoryEntries = new List<Xfsa2DirectoryEntry>();
            directoryHashes = new List<uint>();
            var fileOffset = 0;
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

                var hash = crc32.ComputeValue(directoryName.ToLower(), sjis);
                var newDirectoryEntry = new Xfsa2DirectoryEntry
                {
                    crc32 = string.IsNullOrEmpty(fileGroup.Key.ToRelative().FullName) ? 0xFFFFFFFF : hash,

                    directoryCount = (short)groupedFiles.Count(gf => fileGroup.Key != gf.Key && gf.Key.IsInDirectory(fileGroup.Key, false)),

                    fileCount = (short)fileGroupEntries.Length,
                    FirstFileIndex = (short)fileIndex,

                    directoryNameOffset = directoryNameOffset,
                    fileNameStartOffset = (int)nameBw.BaseStream.Position
                };
                if (newDirectoryEntry.crc32 != 0xFFFFFFFF)
                    directoryHashes.Add(newDirectoryEntry.crc32);
                directoryEntries.Add(newDirectoryEntry);

                // Write file names in alphabetic order
                foreach (var file in fileGroupEntries)
                {
                    var fileEntry = (XfsaArchiveFile<Xfsa2FileEntry>)file;
                    fileEntry.Entry.NameOffset = (int)(nameBw.BaseStream.Position - newDirectoryEntry.fileNameStartOffset);
                    fileEntry.Entry.crc32 = crc32.ComputeValue(fileEntry.FilePath.GetName().ToLower(), sjis);
                    fileEntry.Entry.FileOffset = fileOffset;
                    fileEntry.Entry.FileSize = (int)fileEntry.FileSize;

                    fileOffset = (int)((fileOffset + fileEntry.FileSize + 15) & ~15);

                    nameBw.WriteString(fileEntry.FilePath.GetName(), sjis, false);
                }

                // Add file entries in order of ascending hash
                fileInfos.AddRange(fileGroupEntries.OrderBy(x => ((XfsaArchiveFile<Xfsa2FileEntry>)x).Entry.crc32));
            }

            fileEntries = fileInfos.Cast<XfsaArchiveFile<Xfsa2FileEntry>>().ToArray();

            // Order directory entries by hash and set directoryIndex accordingly
            var directoryIndex = 0;
            directoryEntries = directoryEntries.OrderBy(x => x.crc32).Select(x =>
            {
                x.FirstDirectoryIndex = (short)directoryIndex;
                directoryIndex += x.directoryCount;
                return x;
            }).ToList();
        }

        private XfsaHeader ReadHeader(BinaryReaderX reader)
        {
            return new XfsaHeader
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
                unk1 = reader.ReadInt32()
            };
        }

        private Xfsa2DirectoryEntry[] ReadDirectoryEntries(BinaryReaderX reader, int count)
        {
            var result = new Xfsa2DirectoryEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadDirectoryEntry(reader);

            return result;
        }

        private Xfsa2DirectoryEntry ReadDirectoryEntry(BinaryReaderX reader)
        {
            return new Xfsa2DirectoryEntry
            {
                crc32 = reader.ReadUInt32(),
                fileCount = reader.ReadInt32(),
                fileNameStartOffset = reader.ReadInt32(),
                tmp1 = reader.ReadUInt32(),
                directoryCount = reader.ReadInt32(),
                directoryNameOffset = reader.ReadInt32()
            };
        }

        private uint[] ReadDirectoryHashes(BinaryReaderX reader, int count)
        {
            var result = new uint[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadUInt32();

            return result;
        }

        private Xfsa2FileEntry[] ReadFileEntries(BinaryReaderX reader, int count)
        {
            var result = new Xfsa2FileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadFileEntry(reader);

            return result;
        }

        private Xfsa2FileEntry ReadFileEntry(BinaryReaderX reader)
        {
            return new Xfsa2FileEntry
            {
                crc32 = reader.ReadUInt32(),
                tmp1 = reader.ReadUInt32(),
                tmp2 = reader.ReadUInt32()
            };
        }

        private void WriteHeader(XfsaHeader header, BinaryWriterX writer)
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
            writer.Write(header.unk1);
        }

        private Stream WriteDirectoryEntries(IList<Xfsa2DirectoryEntry> entries)
        {
            var stream = new MemoryStream();
            using var writer = new BinaryWriterX(stream, true);

            foreach (Xfsa2DirectoryEntry entry in entries)
                WriteDirectoryEntry(entry, writer);

            return stream;
        }

        private void WriteDirectoryEntry(Xfsa2DirectoryEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.crc32);
            writer.Write(entry.fileCount);
            writer.Write(entry.fileNameStartOffset);
            writer.Write(entry.tmp1);
            writer.Write(entry.directoryCount);
            writer.Write(entry.directoryNameOffset);
        }

        private Stream WriteDirectoryHashes(IList<uint> entries)
        {
            var stream = new MemoryStream();
            using var writer = new BinaryWriterX(stream, true);

            foreach (uint entry in entries)
                writer.Write(entry);

            return stream;
        }

        private Stream WriteFileEntries(IList<XfsaArchiveFile<Xfsa2FileEntry>> entries)
        {
            var stream = new MemoryStream();
            using var writer = new BinaryWriterX(stream, true);

            foreach (XfsaArchiveFile<Xfsa2FileEntry> entry in entries)
                WriteFileEntry(entry.Entry, writer);

            return stream;
        }

        private void WriteFileEntry(Xfsa2FileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.crc32);
            writer.Write(entry.tmp1);
            writer.Write(entry.tmp2);
        }
    }
}
