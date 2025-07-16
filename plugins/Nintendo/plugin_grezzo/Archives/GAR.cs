using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_grezzo.Archives
{
    public class GAR
    {
        private const int HeaderSize_ = 0x20;

        private const int Gar2FileTypeEntrySize_ = 0x10;
        private const int Gar2FileEntrySize_ = 0xC;

        private const int Gar5FileTypeEntrySize_ = 0x20;
        private const int Gar5FileTypeInfoSize_ = 0xC;
        private const int Gar5FileEntrySize_ = 0x10;

        private byte _headerVersion;
        private string _headerString;

        private IList<(Gar5FileTypeEntry, string)> _fileTypeEntries;
        private IList<Gar5FileTypeInfo> _fileTypeInfos;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read Header
            var header = ReadHeader(br);

            // Parse rest of the file
            _headerVersion = header.version;
            _headerString = header.hold0;
            switch (_headerVersion)
            {
                case 2:
                    return ParseGar2(br, header);

                case 5:
                    return ParseGar5(br, header);

                default:
                    throw new InvalidOperationException($"GAR with version {_headerVersion} is not supported.");
            }
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            switch (_headerVersion)
            {
                case 2:
                    SaveGar2(output, files);
                    break;

                case 5:
                    SaveGar5(output, files);
                    break;

                default:
                    throw new InvalidOperationException($"GAR with version {_headerVersion} is not supported.");
            }
        }

        private List<IArchiveFile> ParseGar2(BinaryReaderX br, GarHeader header)
        {
            // Read file type entries
            var fileTypeEntries = ReadGar2Entries(br, header.fileTypeCount);

            var result = new List<IArchiveFile>();
            foreach (var fileTypeEntry in fileTypeEntries)
            {
                if (fileTypeEntry.fileIndexOffset < 0)
                    continue;

                // Read file entry indices
                br.BaseStream.Position = fileTypeEntry.fileIndexOffset;
                var fileIndices = ReadIntegers(br, fileTypeEntry.fileCount);

                // Read file entries
                br.BaseStream.Position = header.fileEntryOffset;
                var fileEntries = ReadGar2FileEntries(br, header.fileCount);

                // Read file offsets
                br.BaseStream.Position = header.fileOffsetsOffset;
                var fileOffsets = ReadIntegers(br, header.fileCount);

                // Add files
                foreach (var fileIndex in fileIndices)
                {
                    var fileStream = new SubStream(br.BaseStream, fileOffsets[fileIndex], fileEntries[fileIndex].fileSize);

                    br.BaseStream.Position = fileEntries[fileIndex].fileNameOffset;
                    var fileName = br.ReadNullTerminatedString();

                    result.Add(new ArchiveFile(new ArchiveFileInfo
                    {
                        FilePath = fileName,
                        FileData = fileStream
                    }));
                }
            }

            return result;
        }

        private List<IArchiveFile> ParseGar5(BinaryReaderX br, GarHeader header)
        {
            // Read file type entries
            _fileTypeEntries = new List<(Gar5FileTypeEntry, string)>();
            var fileTypeEntries = ReadGar5FileTypeEntries(br, header.fileTypeCount);

            // Read directory infos
            var fileTypeInfoPosition = fileTypeEntries.Where(x => x.fileTypeInfoOffset > 0).Min(x => x.fileTypeInfoOffset);
            var fileTypeInfoLength = header.fileEntryOffset - fileTypeInfoPosition;
            br.BaseStream.Position = fileTypeInfoPosition;
            _fileTypeInfos = ReadGar5FileTypeInfos(br, fileTypeInfoLength / Gar5FileTypeInfoSize_);

            var result = new List<IArchiveFile>();
            foreach (var fileTypeEntry in fileTypeEntries)
            {
                // Read file type name
                br.BaseStream.Position = fileTypeEntry.fileTypeNameOffset;
                var fileTypeName = br.ReadNullTerminatedString();
                _fileTypeEntries.Add((fileTypeEntry, "." + fileTypeName));

                // Read file entries
                br.BaseStream.Position = header.fileEntryOffset;
                var fileEntries = ReadGar5FileEntries(br, header.fileCount);

                // Add files
                if (fileTypeEntry.fileEntryIndex >= 0)
                {
                    var fileEntryIndexEnd = fileTypeEntry.fileEntryIndex + fileTypeEntry.fileCount;
                    for (var i = fileTypeEntry.fileEntryIndex; i < fileEntryIndexEnd; i++)
                    {
                        var fileStream = new SubStream(br.BaseStream, fileEntries[i].fileOffset, fileEntries[i].fileSize);

                        br.BaseStream.Position = fileEntries[i].fileNameOffset;
                        var fileName = br.ReadNullTerminatedString();

                        result.Add(new ArchiveFile(new ArchiveFileInfo
                        {
                            FilePath = fileName + "." + fileTypeName,
                            FileData = fileStream
                        }));
                    }
                }
            }

            return result;
        }

        private void SaveGar2(Stream output, List<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            var fileTypes = files.Select(x => x.FilePath.GetExtensionWithDot()).Distinct().ToArray();

            var fileTypeEntryPosition = HeaderSize_;
            var fileTypeNamePosition = fileTypeEntryPosition + fileTypes.Length * Gar2FileTypeEntrySize_;

            // Write directory entries
            var fileInfos = new List<IArchiveFile>();

            var fileIndex = 0;
            var fileTypeEntryOffset = fileTypeEntryPosition;
            var fileTypeNameOffset = fileTypeNamePosition;
            foreach (var fileType in fileTypes)
            {
                // Write directory name
                bw.BaseStream.Position = fileTypeNameOffset;
                bw.WriteString(fileType.Substring(1, fileType.Length - 1), Encoding.ASCII, false);
                bw.WriteAlignment(4);

                // Select files in directory
                var relevantFiles = files.Where(x => x.FilePath.GetExtensionWithDot() == fileType).ToArray();
                fileInfos.AddRange(relevantFiles);

                // Write file indices
                var fileIndexOffset = (int)bw.BaseStream.Position;
                WriteIntegers(Enumerable.Range(fileIndex, relevantFiles.Length).ToArray(), bw);
                fileIndex += relevantFiles.Length;

                var newDirectoryNameOffset = (int)bw.BaseStream.Position;

                // Write directory entry
                var entry = new Gar2FileTypeEntry
                {
                    fileCount = relevantFiles.Length,
                    fileTypeNameOffset = fileTypeNameOffset,
                    fileIndexOffset = fileIndexOffset,
                    unk1 = -1
                };

                bw.BaseStream.Position = fileTypeEntryOffset;
                WriteGar2Entry(entry, bw);

                fileTypeNameOffset = newDirectoryNameOffset;
                fileTypeEntryOffset += Gar2FileTypeEntrySize_;
            }

            var fileEntryPosition = fileTypeNameOffset;

            // Write file entries
            var fileEntryOffset = fileEntryPosition;
            var fileNameOffset = fileEntryPosition + fileInfos.Count * Gar2FileEntrySize_;
            foreach (var fileInfo in fileInfos)
            {
                bw.BaseStream.Position = fileNameOffset;

                // Write file name
                bw.WriteString(fileInfo.FilePath.GetName(), Encoding.ASCII, false);

                // Write name
                var nameOffset = (int)bw.BaseStream.Position;
                bw.WriteString(fileInfo.FilePath.GetNameWithoutExtension(), Encoding.ASCII, false);
                bw.WriteAlignment(4);

                var newFileNameOffset = (int)bw.BaseStream.Position;

                // Write file entry
                var entry = new Gar2FileEntry
                {
                    fileSize = (uint)fileInfo.FileSize,
                    nameOffset = nameOffset,
                    fileNameOffset = fileNameOffset
                };

                bw.BaseStream.Position = fileEntryOffset;
                WriteGar2FileEntry(entry, bw);

                fileNameOffset = newFileNameOffset;
                fileEntryOffset += Gar2FileEntrySize_;
            }

            var fileOffsetPosition = fileNameOffset;
            var dataPosition = fileOffsetPosition + fileInfos.Count * 4;

            // Write file offsets
            bw.BaseStream.Position = fileOffsetPosition;
            foreach (var fileInfo in fileInfos)
            {
                bw.Write(dataPosition);
                dataPosition = (int)((dataPosition + fileInfo.FileSize + 3) & ~3);
            }

            // Write file data
            foreach (var fileInfo in fileInfos)
            {
                fileInfo.WriteFileData(output);
                bw.WriteAlignment(4);
            }

            // Write header
            var header = new GarHeader
            {
                magic = "GAR",

                fileTypeCount = (short)fileTypes.Length,
                fileCount = (short)fileInfos.Count,

                fileSize = (uint)bw.BaseStream.Length,

                fileTypeEntryOffset = fileTypeEntryPosition,
                fileEntryOffset = fileEntryPosition,
                fileOffsetsOffset = fileOffsetPosition,

                version = _headerVersion,
                hold0 = _headerString
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);
        }

        private void SaveGar5(Stream output, List<IArchiveFile> files)
        {
            var fileTypeEntryPosition = HeaderSize_;
            var fileTypeNamePosition = fileTypeEntryPosition + _fileTypeEntries.Count * Gar5FileTypeEntrySize_;

            using var bw = new BinaryWriterX(output);

            // Write file type entries
            var fileInfos = new List<IArchiveFile>();

            var fileEntryIndex = 0;
            var fileTypeNameOffset = fileTypeNamePosition;
            var fileTypeEntryOffset = fileTypeEntryPosition;
            foreach (var fileTypeEntry in _fileTypeEntries)
            {
                var relevantFiles = files.Where(x => x.FilePath.GetExtensionWithDot() == fileTypeEntry.Item2).ToArray();
                fileInfos.AddRange(relevantFiles);

                // Write file type name
                bw.BaseStream.Position = fileTypeNameOffset;
                var fileTypeName = fileTypeEntry.Item2.Substring(1, fileTypeEntry.Item2.Length - 1);
                bw.WriteString(fileTypeName, Encoding.ASCII);

                // Update entry information
                fileTypeEntry.Item1.fileCount = relevantFiles.Length;
                fileTypeEntry.Item1.fileEntryIndex = relevantFiles.Length == 0 ? -1 : fileEntryIndex;
                fileTypeEntry.Item1.fileTypeNameOffset = fileTypeNameOffset;

                fileTypeNameOffset = (int)bw.BaseStream.Position;
                fileEntryIndex += relevantFiles.Length;

                // Write file type entry
                bw.BaseStream.Position = fileTypeEntryOffset;
                WriteGar5FileTypeEntry(fileTypeEntry.Item1, bw);
                bw.WriteAlignment(0x20);

                fileTypeEntryOffset = (int)bw.BaseStream.Position;
            }

            var fileTypeInfoPosition = (fileTypeNameOffset + 3) & ~3;

            // Write file type infos
            bw.BaseStream.Position = fileTypeInfoPosition;
            WriteGar5FileTypeInfos(_fileTypeInfos, bw);

            var fileEntryPosition = (int)bw.BaseStream.Position;
            var fileNamePosition = fileEntryPosition + fileInfos.Count * Gar5FileEntrySize_;

            // Write file names
            var fileEntries = new List<Gar5FileEntry>();
            var fileNameOffset = fileNamePosition;
            foreach (var fileInfo in fileInfos)
            {
                // Write file name
                bw.BaseStream.Position = fileNameOffset;
                bw.WriteString(fileInfo.FilePath.GetNameWithoutExtension(), Encoding.ASCII, false);

                // Create file entry
                fileEntries.Add(new Gar5FileEntry
                {
                    fileSize = (int)fileInfo.FileSize,
                    fileNameOffset = fileNameOffset,
                    unk1 = -1
                });

                fileNameOffset = (int)bw.BaseStream.Position;
            }

            var dataPosition = (fileNameOffset + 0xF) & ~0xF;

            // Write file data
            bw.BaseStream.Position = dataPosition;
            for (var i = 0; i < fileInfos.Count; i++)
            {
                fileEntries[i].fileOffset = (int)bw.BaseStream.Position;

                fileInfos[i].WriteFileData(bw.BaseStream);
                bw.WriteAlignment(4);
            }

            // Write file entries
            bw.BaseStream.Position = fileEntryPosition;
            WriteGar5FileEntries(fileEntries, bw);

            // Write header
            var header = new GarHeader
            {
                magic = "GAR",

                fileTypeCount = (short)_fileTypeEntries.Count,
                fileCount = (short)fileInfos.Count,

                fileTypeEntryOffset = fileTypeEntryPosition,
                fileEntryOffset = fileEntryPosition,
                fileOffsetsOffset = dataPosition,

                fileSize = (uint)bw.BaseStream.Length,

                version = _headerVersion,
                hold0 = _headerString
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);
        }

        private GarHeader ReadHeader(BinaryReaderX reader)
        {
            return new GarHeader
            {
                magic = reader.ReadString(3),
                version = reader.ReadByte(),
                fileSize = reader.ReadUInt32(),
                fileTypeCount = reader.ReadInt16(),
                fileCount = reader.ReadInt16(),
                fileTypeEntryOffset = reader.ReadInt32(),
                fileEntryOffset = reader.ReadInt32(),
                fileOffsetsOffset = reader.ReadInt32(),
                hold0 = reader.ReadString(8)
            };
        }

        private Gar2FileTypeEntry[] ReadGar2Entries(BinaryReaderX reader, int count)
        {
            var result = new Gar2FileTypeEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadGar2Entry(reader);

            return result;
        }

        private Gar2FileTypeEntry ReadGar2Entry(BinaryReaderX reader)
        {
            return new Gar2FileTypeEntry
            {
                fileCount = reader.ReadInt32(),
                fileIndexOffset = reader.ReadInt32(),
                fileTypeNameOffset = reader.ReadInt32(),
                unk1 = reader.ReadInt32()
            };
        }

        private Gar2FileEntry[] ReadGar2FileEntries(BinaryReaderX reader, int count)
        {
            var result = new Gar2FileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadGar2FileEntry(reader);

            return result;
        }

        private Gar2FileEntry ReadGar2FileEntry(BinaryReaderX reader)
        {
            return new Gar2FileEntry
            {
                fileSize = reader.ReadUInt32(),
                nameOffset = reader.ReadInt32(),
                fileNameOffset = reader.ReadInt32()
            };
        }

        private Gar5FileTypeEntry[] ReadGar5FileTypeEntries(BinaryReaderX reader, int count)
        {
            var result = new Gar5FileTypeEntry[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadGar5FileTypeEntry(reader);
                reader.SeekAlignment(0x20);
            }

            return result;
        }

        private Gar5FileTypeEntry ReadGar5FileTypeEntry(BinaryReaderX reader)
        {
            return new Gar5FileTypeEntry
            {
                fileCount = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                fileEntryIndex = reader.ReadInt32(),
                fileTypeNameOffset = reader.ReadInt32(),
                fileTypeInfoOffset = reader.ReadInt32()
            };
        }

        private Gar5FileEntry[] ReadGar5FileEntries(BinaryReaderX reader, int count)
        {
            var result = new Gar5FileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadGar5FileEntry(reader);

            return result;
        }

        private Gar5FileEntry ReadGar5FileEntry(BinaryReaderX reader)
        {
            return new Gar5FileEntry
            {
                fileSize = reader.ReadInt32(),
                fileOffset = reader.ReadInt32(),
                fileNameOffset = reader.ReadInt32(),
                unk1 = reader.ReadInt32()
            };
        }

        private Gar5FileTypeInfo[] ReadGar5FileTypeInfos(BinaryReaderX reader, int count)
        {
            var result = new Gar5FileTypeInfo[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadGar5FileTypeInfo(reader);

            return result;
        }

        private Gar5FileTypeInfo ReadGar5FileTypeInfo(BinaryReaderX reader)
        {
            return new Gar5FileTypeInfo
            {
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadInt16(),
                unk4 = reader.ReadInt16()
            };
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteHeader(GarHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.fileSize);
            writer.Write(header.fileTypeCount);
            writer.Write(header.fileCount);
            writer.Write(header.fileTypeEntryOffset);
            writer.Write(header.fileEntryOffset);
            writer.Write(header.fileOffsetsOffset);
            writer.WriteString(header.hold0, writeNullTerminator: false);
        }

        private void WriteGar2Entry(Gar2FileTypeEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.fileCount);
            writer.Write(entry.fileIndexOffset);
            writer.Write(entry.fileTypeNameOffset);
            writer.Write(entry.unk1);
        }

        private void WriteGar2FileEntry(Gar2FileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.fileSize);
            writer.Write(entry.nameOffset);
            writer.Write(entry.fileNameOffset);
        }

        private void WriteGar5FileTypeEntry(Gar5FileTypeEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.fileCount);
            writer.Write(entry.unk1);
            writer.Write(entry.fileEntryIndex);
            writer.Write(entry.fileTypeNameOffset);
            writer.Write(entry.fileTypeInfoOffset);
        }

        private void WriteGar5FileTypeInfos(IList<Gar5FileTypeInfo> entries, BinaryWriterX writer)
        {
            foreach (Gar5FileTypeInfo entry in entries)
                WriteGar5FileTypeInfo(entry, writer);
        }

        private void WriteGar5FileTypeInfo(Gar5FileTypeInfo entry, BinaryWriterX writer)
        {
            writer.Write(entry.unk1);
            writer.Write(entry.unk2);
            writer.Write(entry.unk3);
            writer.Write(entry.unk4);
        }

        private void WriteGar5FileEntries(IList<Gar5FileEntry> entries, BinaryWriterX writer)
        {
            foreach (Gar5FileEntry entry in entries)
                WriteGar5FileEntry(entry, writer);
        }

        private void WriteGar5FileEntry(Gar5FileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.fileSize);
            writer.Write(entry.fileOffset);
            writer.Write(entry.fileNameOffset);
            writer.Write(entry.unk1);
        }

        private void WriteIntegers(int[] entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }
    }
}
