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
        private const int Gar5FileTypeInfoSize_ = 0xc;
        private const int Gar5FileEntrySize_ = 0x10;

        private byte _headerVersion;
        private string _headerString;

        private IList<(Gar5FileTypeEntry, string)> _fileTypeEntries;
        private IList<Gar5FileTypeInfo> _fileTypeInfos;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true);

            // Read Header
            var header = typeReader.Read<GarHeader>(br);

            // Parse rest of the file
            _headerVersion = header.version;
            _headerString = header.hold0;
            switch (_headerVersion)
            {
                case 2:
                    return ParseGar2(typeReader, br, header);

                case 5:
                    return ParseGar5(typeReader, br, header);

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

        private List<IArchiveFile> ParseGar2(BinaryTypeReader typeReader, BinaryReaderX br, GarHeader header)
        {
            // Read file type entries
            var fileTypeEntries = typeReader.ReadMany<Gar2FileTypeEntry>(br, header.fileTypeCount);

            var result = new List<IArchiveFile>();
            foreach (var fileTypeEntry in fileTypeEntries)
            {
                if (fileTypeEntry.fileIndexOffset < 0)
                    continue;

                // Read file entry indices
                br.BaseStream.Position = fileTypeEntry.fileIndexOffset;
                var fileIndices = typeReader.ReadMany<int>(br, fileTypeEntry.fileCount);

                // Read file entries
                br.BaseStream.Position = header.fileEntryOffset;
                var fileEntries = typeReader.ReadMany<Gar2FileEntry>(br, header.fileCount);

                // Read file offsets
                br.BaseStream.Position = header.fileOffsetsOffset;
                var fileOffsets = typeReader.ReadMany<int>(br, header.fileCount);

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

        private List<IArchiveFile> ParseGar5(BinaryTypeReader typeReader, BinaryReaderX br, GarHeader header)
        {
            // Read file type entries
            _fileTypeEntries = new List<(Gar5FileTypeEntry, string)>();
            var fileTypeEntries = typeReader.ReadMany<Gar5FileTypeEntry>(br, header.fileTypeCount);

            // Read directory infos
            var fileTypeInfoPosition = fileTypeEntries.Where(x => x.fileTypeInfoOffset > 0).Min(x => x.fileTypeInfoOffset);
            var fileTypeInfoLength = header.fileEntryOffset - fileTypeInfoPosition;
            br.BaseStream.Position = fileTypeInfoPosition;
            _fileTypeInfos = typeReader.ReadMany<Gar5FileTypeInfo>(br, fileTypeInfoLength / Gar5FileTypeInfoSize_);

            var result = new List<IArchiveFile>();
            foreach (var fileTypeEntry in fileTypeEntries)
            {
                // Read file type name
                br.BaseStream.Position = fileTypeEntry.fileTypeNameOffset;
                var fileTypeName = br.ReadNullTerminatedString();
                _fileTypeEntries.Add((fileTypeEntry, "." + fileTypeName));

                // Read file entries
                br.BaseStream.Position = header.fileEntryOffset;
                var fileEntries = typeReader.ReadMany<Gar5FileEntry>(br, header.fileCount);

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
            var typeWriter = new BinaryTypeWriter();
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
                typeWriter.WriteMany(Enumerable.Range(fileIndex, relevantFiles.Length), bw);
                fileIndex += relevantFiles.Length;

                var newDirectoryNameOffset = (int)bw.BaseStream.Position;

                // Write directory entry
                bw.BaseStream.Position = fileTypeEntryOffset;
                typeWriter.Write(new Gar2FileTypeEntry
                {
                    fileCount = relevantFiles.Length,
                    fileTypeNameOffset = fileTypeNameOffset,
                    fileIndexOffset = fileIndexOffset
                }, bw);

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
                bw.BaseStream.Position = fileEntryOffset;
                typeWriter.Write(new Gar2FileEntry
                {
                    fileSize = (uint)fileInfo.FileSize,
                    nameOffset = nameOffset,
                    fileNameOffset = fileNameOffset
                }, bw);

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
            bw.BaseStream.Position = 0;
            typeWriter.Write(new GarHeader
            {
                fileTypeCount = (short)fileTypes.Length,
                fileCount = (short)fileInfos.Count,

                fileSize = (uint)bw.BaseStream.Length,

                fileTypeEntryOffset = fileTypeEntryPosition,
                fileEntryOffset = fileEntryPosition,
                fileOffsetsOffset = fileOffsetPosition,

                version = _headerVersion,
                hold0 = _headerString
            }, bw);
        }

        private void SaveGar5(Stream output, List<IArchiveFile> files)
        {
            var fileTypeEntryPosition = HeaderSize_;
            var fileTypeNamePosition = fileTypeEntryPosition + _fileTypeEntries.Count * Gar5FileTypeEntrySize_;

            var typeWriter = new BinaryTypeWriter();
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
                bw.WriteString(fileTypeName, Encoding.ASCII, false);

                // Update entry information
                fileTypeEntry.Item1.fileCount = relevantFiles.Length;
                fileTypeEntry.Item1.fileEntryIndex = relevantFiles.Length == 0 ? -1 : fileEntryIndex;
                fileTypeEntry.Item1.fileTypeNameOffset = fileTypeNameOffset;

                fileTypeNameOffset = (int)bw.BaseStream.Position;
                fileEntryIndex += relevantFiles.Length;

                // Write file type entry
                bw.BaseStream.Position = fileTypeEntryOffset;
                typeWriter.Write(fileTypeEntry, bw);

                fileTypeEntryOffset = (int)bw.BaseStream.Position;
            }

            var fileTypeInfoPosition = (fileTypeNameOffset + 3) & ~3;

            // Write file type infos
            bw.BaseStream.Position = fileTypeInfoPosition;
            typeWriter.WriteMany(_fileTypeInfos, bw);

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
                    fileNameOffset = fileNameOffset
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
            typeWriter.WriteMany(fileEntries, bw);

            // Write header
            bw.BaseStream.Position = 0;
            typeWriter.Write(new GarHeader
            {
                fileTypeCount = (short)_fileTypeEntries.Count,
                fileCount = (short)fileInfos.Count,

                fileTypeEntryOffset = fileTypeEntryPosition,
                fileEntryOffset = fileEntryPosition,
                fileOffsetsOffset = dataPosition,

                fileSize = (uint)bw.BaseStream.Length,

                version = _headerVersion,
                hold0 = _headerString
            }, bw);
        }
    }
}
