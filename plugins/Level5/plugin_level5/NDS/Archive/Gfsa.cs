using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Extensions;
using Kryptography.Checksum.Crc;
using plugin_level5.Common.Compression;
using plugin_level5.N3DS.Archive;

namespace plugin_level5.NDS.Archive
{
    public class Gfsa
    {
        private const int HeaderSize_ = 40;
        private const int DirectoryEntrySize_ = 8;
        private const int FileEntrySize_ = 8;

        private GfsaHeader _header;
        private byte[] _unkTable;

        public List<GfsaArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read tables
            var directoryEntriesReader = XfsaSupport.GetDecompressedTableEntries(input,
                _header.directoryOffset, _header.fileOffset - _header.directoryOffset);
            var directoryEntries = ReadDirectoryEntries(directoryEntriesReader, _header.directoryCount);

            var entriesReader = XfsaSupport.GetDecompressedTableEntries(input,
                _header.fileOffset, _header.unkOffset - _header.fileOffset);
            var entries = ReadFileEntries(entriesReader, _header.fileCount);

            input.Position = _header.unkOffset;
            _unkTable = br.ReadBytes(_header.stringOffset - _header.unkOffset);

            // Read strings
            var nameComp = new SubStream(input, _header.stringOffset, _header.fileDataOffset - _header.stringOffset);
            var nameStream = new MemoryStream();
            Level5Compressor.Decompress(nameComp, nameStream);
            var (directories, files) = ReadStrings(nameStream);

            // Add files
            var result = new List<GfsaArchiveFile>();
            foreach (var dirEntry in directoryEntries)
            {
                var dirName = directories.FirstOrDefault(x => x.Hash == dirEntry.hash)?.Value;

                for (var fileIndex = dirEntry.fileIndex; fileIndex < dirEntry.fileIndex + dirEntry.fileCount; fileIndex++)
                {
                    var fileEntry = entries[fileIndex];
                    var fileName = files.Skip(dirEntry.fileIndex).Take(dirEntry.fileCount).FirstOrDefault(x => x.Hash == fileEntry.hash)?.Value;

                    var fileData = new SubStream(input, _header.fileDataOffset + fileEntry.Offset, fileEntry.Size);
                    result.Add(CreateAfi(fileData, Path.Combine(dirName, fileName), fileEntry));
                }
            }

            return result;
        }

        public void Save(Stream output, List<GfsaArchiveFile> files)
        {
            output.Position = HeaderSize_;

            // Write directory table
            _header.directoryOffset = HeaderSize_;
            _header.directoryCount = files.Select(x => x.FilePath.GetDirectory()).Distinct().Count();
            WriteDirectoryTable(output, files);

            // Write file entries
            _header.fileOffset = (int)output.Position;
            _header.fileCount = files.Count;
            WriteFileTable(output, files);

            // Write unknown table
            _header.unkOffset = (int)output.Position;
            output.Write(_unkTable);

            // Write strings
            _header.stringOffset = (int)output.Position;
            WriteStrings(output, files);

            // Write files
            _header.fileDataOffset = (int)output.Position;
            WriteFiles(output, files);

            // Write header
            _header.decompressedTableSize = _header.directoryCount * DirectoryEntrySize_ + _header.fileCount * FileEntrySize_;
            output.Position = 0;

            using var bw = new BinaryWriterX(output);
            WriteHeader(_header, bw);
        }

        #region Load

        private GfsaHeader ReadHeader(BinaryReaderX reader)
        {
            return new GfsaHeader
            {
                magic = reader.ReadString(4),
                directoryOffset = reader.ReadInt32(),
                fileOffset = reader.ReadInt32(),
                unkOffset = reader.ReadInt32(),
                stringOffset = reader.ReadInt32(),
                fileDataOffset = reader.ReadInt32(),
                directoryCount = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                decompressedTableSize = reader.ReadInt32(),
                unk4 = reader.ReadInt32()
            };
        }

        private GfsaDirectoryEntry[] ReadDirectoryEntries(BinaryReaderX reader, int count)
        {
            var result = new GfsaDirectoryEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadDirectoryEntry(reader);

            return result;
        }

        private GfsaDirectoryEntry ReadDirectoryEntry(BinaryReaderX reader)
        {
            return new GfsaDirectoryEntry
            {
                hash = reader.ReadUInt16(),
                fileCount = reader.ReadInt16(),
                fileIndex = reader.ReadInt32()
            };
        }

        private GfsaFileEntry[] ReadFileEntries(BinaryReaderX reader, int count)
        {
            var result = new GfsaFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadFileEntry(reader);

            return result;
        }

        private GfsaFileEntry ReadFileEntry(BinaryReaderX reader)
        {
            return new GfsaFileEntry
            {
                hash = reader.ReadUInt16(),
                offLow = reader.ReadUInt16(),
                sizeLow = reader.ReadUInt16(),
                offHigh = reader.ReadByte(),
                sizeHigh = reader.ReadByte()
            };
        }

        private (GfsaString[], GfsaString[]) ReadStrings(Stream stringStream)
        {
            var directories = new List<string>();
            var files = new List<string>();

            stringStream.Position = 0;

            using var br = new BinaryReaderX(stringStream);
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                var value = br.ReadNullTerminatedString();
                if (value.EndsWith('/'))
                    directories.Add(value);
                else
                    files.Add(value);
            }

            return (GetGfsaStrings(directories), GetGfsaStrings(files));
        }

        private GfsaString[] GetGfsaStrings(IList<string> values)
        {
            var crc16 = Crc16.X25;
            return values
                .Select(x => new GfsaString(x, crc16.ComputeValue(x)))
                .ToArray();
        }

        private GfsaArchiveFile CreateAfi(Stream file, string name, GfsaFileEntry entry)
        {
            ArchiveFileInfo fileInfo;

            var method = Level5Compressor.PeekCompressionMethod(file);
            if (method != Level5CompressionMethod.NoCompression)
            {
                fileInfo = new CompressedArchiveFileInfo
                {
                    FileData = file,
                    FilePath = name,
                    Compression = Level5Compressor.GetCompression(method),
                    DecompressedSize = Level5Compressor.PeekDecompressedSize(file)
                };
                return new GfsaArchiveFile(fileInfo, entry);
            }

            fileInfo = new ArchiveFileInfo
            {
                FileData = new SubStream(file, 4, file.Length - 4),
                FilePath = name
            };
            return new GfsaArchiveFile(fileInfo, entry);
        }

        #endregion

        #region Save

        private void WriteHeader(GfsaHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.directoryOffset);
            writer.Write(header.fileOffset);
            writer.Write(header.unkOffset);
            writer.Write(header.stringOffset);
            writer.Write(header.fileDataOffset);
            writer.Write(header.directoryCount);
            writer.Write(header.fileCount);
            writer.Write(header.decompressedTableSize);
            writer.Write(header.unk4);
        }

        private Stream WriteDirectoryEntries(IList<GfsaDirectoryEntry> entries)
        {
            var stream = new MemoryStream();
            using var writer = new BinaryWriterX(stream, true);

            foreach (GfsaDirectoryEntry entry in entries)
                WriteDirectoryEntry(entry, writer);

            return stream;
        }

        private void WriteDirectoryEntry(GfsaDirectoryEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.hash);
            writer.Write(entry.fileCount);
            writer.Write(entry.fileIndex);
        }

        private Stream WriteFileEntries(IList<GfsaFileEntry> entries)
        {
            var stream = new MemoryStream();
            using var writer = new BinaryWriterX(stream, true);

            foreach (GfsaFileEntry entry in entries)
                WriteFileEntry(entry, writer);

            return stream;
        }

        private void WriteFileEntry(GfsaFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.hash);
            writer.Write(entry.offLow);
            writer.Write(entry.sizeLow);
            writer.Write(entry.offHigh);
            writer.Write(entry.sizeHigh);
        }

        private void WriteDirectoryTable(Stream output, IList<GfsaArchiveFile> files)
        {
            var crc16 = Crc16.X25;

            var directoryEntries = new List<GfsaDirectoryEntry>();

            var fileIndex = 0;
            foreach (var group in OrderFiles(files).GroupBy(x => x.FilePath.GetDirectory().ToRelative() + "/"))
            {
                directoryEntries.Add(new GfsaDirectoryEntry
                {
                    hash = crc16.ComputeValue(group.Key),
                    fileIndex = fileIndex,
                    fileCount = (short)group.Count()
                });

                fileIndex += directoryEntries.Last().fileCount;
            }

            var directoryEntriesStream = WriteDirectoryEntries(directoryEntries.OrderBy(x => x.hash).ToArray());
            XfsaSupport.WriteCompressedStream(output, directoryEntriesStream);

            while (output.Position % 4 > 0)
                output.WriteByte(0);
        }

        private void WriteFileTable(Stream output, IList<GfsaArchiveFile> files)
        {
            var crc16 = Crc16.X25;

            var fileEntries = new List<GfsaFileEntry>();

            var fileOffset = 0;
            foreach (var fileGroup in OrderFiles(files).GroupBy(x => x.FilePath.GetDirectory().ToRelative() + "/"))
            {
                var localGroup = fileGroup.Select(file =>
                {
                    var entry = new GfsaFileEntry
                    {
                        hash = crc16.ComputeValue(file.FilePath.GetName()),
                        Offset = fileOffset,
                        Size = (int)file.CompressedSize
                    };

                    fileOffset += (int)((file.CompressedSize + 3) & ~3);

                    return entry;
                });

                fileEntries.AddRange(localGroup.OrderBy(x => x.hash));
            }

            var fileEntriesStream = WriteFileEntries(fileEntries);
            XfsaSupport.WriteCompressedStream(output, fileEntriesStream);

            while (output.Position % 4 > 0) output.WriteByte(0);
        }

        private void WriteStrings(Stream output, IList<GfsaArchiveFile> files)
        {
            var directories = new HashSet<string>();
            var fileNames = new List<string>(files.Count);

            foreach (var file in OrderFiles(files))
            {
                directories.Add(file.FilePath.GetDirectory().ToRelative().FullName + "/");
                fileNames.Add(file.FilePath.GetName());
            }

            var strings = new MemoryStream();
            using var bw = new BinaryWriterX(strings, true);
            foreach (var s in directories) bw.WriteString(s);
            foreach (var s in fileNames) bw.WriteString(s);

            var compStrings = new MemoryStream();
            XfsaSupport.Compress(strings, compStrings, Level5CompressionMethod.Lz10);

            compStrings.CopyTo(output);

            while (output.Position % 4 > 0) output.WriteByte(0);
        }

        private void WriteFiles(Stream output, IList<GfsaArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output, true);

            foreach (var file in OrderFiles(files))
            {
                if (!file.UsesCompression)
                {
                    var compressionHeader = new[] {
                        (byte)(output.Length << 3),
                        (byte)(output.Length >> 5),
                        (byte)(output.Length >> 13),
                        (byte)(output.Length >> 21) };
                    bw.Write(compressionHeader, 0, 4);
                }

                file.WriteFileData(output, true);

                bw.WriteAlignment(4);
            }
        }

        private IEnumerable<GfsaArchiveFile> OrderFiles(IList<GfsaArchiveFile> files)
        {
            return files.GroupBy(x => x.FilePath.GetDirectory()).SelectMany(x => x.OrderBy(y => y.FilePath.GetName()));
        }

        #endregion
    }
}
