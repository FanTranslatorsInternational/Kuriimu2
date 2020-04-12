using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_grezzo.Archives
{
    public class GAR
    {
        private static int _headerSize = Tools.MeasureType(typeof(GarHeader));

        private static int _gar2DirectoryEntrySize = Tools.MeasureType(typeof(Gar2DirectoryEntry));
        private static int _gar2FileEntrySize = Tools.MeasureType(typeof(Gar2FileEntry));

        private static int _gar5DirectoryEntrySize = 0x20;
        private static int _gar5DirectoryInfoSize = Tools.MeasureType(typeof(Gar5DirectoryInfo));
        private static int _gar5FileEntrySize = Tools.MeasureType(typeof(Gar5FileEntry));

        private byte _headerVersion;
        private string _headerString;

        private IList<(Gar5DirectoryEntry, string)> _directoryEntries;
        private IList<Gar5DirectoryInfo> _directoryInfos;

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read Header
            var header = br.ReadType<GarHeader>();

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

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
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

        private IReadOnlyList<ArchiveFileInfo> ParseGar2(BinaryReaderX br, GarHeader header)
        {
            // Read directory entries
            var directoryEntries = br.ReadMultiple<Gar2DirectoryEntry>(header.directoryCount);

            var result = new List<ArchiveFileInfo>();
            foreach (var directoryEntry in directoryEntries)
            {
                if (directoryEntry.fileIdOffset < 0)
                    continue;

                // Read directory name
                br.BaseStream.Position = directoryEntry.directoryNameOffset;
                var directoryName = br.ReadCStringASCII();

                // Read file entry indices
                br.BaseStream.Position = directoryEntry.fileIdOffset;
                var fileIds = br.ReadMultiple<int>(directoryEntry.fileCount);

                // Read file entries
                br.BaseStream.Position = header.fileEntryOffset;
                var fileEntries = br.ReadMultiple<Gar2FileEntry>(header.fileCount);

                // Read file offsets
                br.BaseStream.Position = header.filePositionOffset;
                var fileOffsets = br.ReadMultiple<int>(header.fileCount);

                // Add files
                foreach (var fileId in fileIds)
                {
                    var fileStream = new SubStream(br.BaseStream, fileOffsets[fileId], fileEntries[fileId].fileSize);

                    br.BaseStream.Position = fileEntries[fileId].fileNameOffset;
                    var fileName = br.ReadCStringASCII();

                    result.Add(new ArchiveFileInfo(fileStream, directoryName + "/" + fileName));
                }
            }

            return result;
        }

        private IReadOnlyList<ArchiveFileInfo> ParseGar5(BinaryReaderX br, GarHeader header)
        {
            // Read directory entries
            _directoryEntries = new List<(Gar5DirectoryEntry, string)>();
            var directoryEntries = br.ReadMultiple<Gar5DirectoryEntry>(header.directoryCount);

            // Read directory infos
            var directoryInfoPosition = directoryEntries.Where(x => x.directoryInfoOffset > 0).Min(x => x.directoryInfoOffset);
            var directoryInfoLength = header.fileEntryOffset - directoryInfoPosition;
            br.BaseStream.Position = directoryInfoPosition;
            _directoryInfos = br.ReadMultiple<Gar5DirectoryInfo>(directoryInfoLength / _gar5DirectoryInfoSize);

            var result = new List<ArchiveFileInfo>();
            foreach (var directoryEntry in directoryEntries)
            {
                // Read directory name
                br.BaseStream.Position = directoryEntry.directoryNameOffset;
                var directoryName = br.ReadCStringASCII();
                _directoryEntries.Add((directoryEntry, directoryName));

                // Read file entries
                br.BaseStream.Position = header.fileEntryOffset;
                var fileEntries = br.ReadMultiple<Gar5FileEntry>(header.fileCount);

                // Add files
                if (directoryEntry.fileEntryIndex >= 0)
                {
                    var fileEntryIndexEnd = directoryEntry.fileEntryIndex + directoryEntry.fileCount;
                    for (var i = directoryEntry.fileEntryIndex; i < fileEntryIndexEnd; i++)
                    {
                        var fileStream = new SubStream(br.BaseStream, fileEntries[i].fileOffset,
                            fileEntries[i].fileSize);

                        br.BaseStream.Position = fileEntries[i].fileNameOffset;
                        var fileName = br.ReadCStringASCII();

                        result.Add(
                            new ArchiveFileInfo(fileStream, directoryName + "/" + fileName + "." + directoryName));
                    }
                }
            }

            return result;
        }

        private void SaveGar2(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            var directories = files.Select(x => x.FilePath.GetDirectory().ToRelative()).Distinct().ToArray();

            var directoryEntryPosition = _headerSize;
            var directoryNamePosition = directoryEntryPosition + directories.Length * _gar2DirectoryEntrySize;

            // Write directory entries
            var fileInfos = new List<ArchiveFileInfo>();

            var fileId = 0;
            var directoryEntryOffset = directoryEntryPosition;
            var directoryNameOffset = directoryNamePosition;
            foreach (var directory in directories)
            {
                // Write directory name
                bw.BaseStream.Position = directoryNameOffset;
                bw.WriteString(directory.FullName, Encoding.ASCII, false);
                bw.WriteAlignment(4);

                // Select files in directory
                var relevantFiles = files.Where(x => x.FilePath.IsInDirectory(directory.ToAbsolute(), true)).ToArray();
                fileInfos.AddRange(relevantFiles);

                // Write file ids
                var fileIdOffset = (int)bw.BaseStream.Position;
                bw.WriteMultiple(Enumerable.Range(fileId, relevantFiles.Length));
                fileId += relevantFiles.Length;

                var newDirectoryNameOffset = (int)bw.BaseStream.Position;

                // Write directory entry
                bw.BaseStream.Position = directoryEntryOffset;
                bw.WriteType(new Gar2DirectoryEntry
                {
                    fileCount = relevantFiles.Length,
                    directoryNameOffset = directoryNameOffset,
                    fileIdOffset = fileIdOffset
                });

                directoryNameOffset = newDirectoryNameOffset;
                directoryEntryOffset += _gar2DirectoryEntrySize;
            }

            var fileEntryPosition = directoryNameOffset;

            // Write file entries
            var fileEntryOffset = fileEntryPosition;
            var fileNameOffset = fileEntryPosition + fileInfos.Count * _gar2FileEntrySize;
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
                bw.WriteType(new Gar2FileEntry
                {
                    fileSize = (uint)fileInfo.FileSize,
                    nameOffset = nameOffset,
                    fileNameOffset = fileNameOffset
                });

                fileNameOffset = newFileNameOffset;
                fileEntryOffset += _gar2FileEntrySize;
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
                fileInfo.SaveFileData(output, null);
                bw.WriteAlignment(4);
            }

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new GarHeader
            {
                directoryCount = (short)directories.Length,
                fileCount = (short)fileInfos.Count,

                fileSize = (uint)bw.BaseStream.Length,

                directoryEntryOffset = directoryEntryPosition,
                fileEntryOffset = fileEntryPosition,
                filePositionOffset = fileOffsetPosition,

                version = _headerVersion,
                hold0 = _headerString
            });
        }

        private void SaveGar5(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
            var directoryEntryPosition = _headerSize;
            var directoryNamePosition = directoryEntryPosition + _directoryEntries.Count * _gar5DirectoryEntrySize;

            using var bw = new BinaryWriterX(output);

            // Write directory entries
            var fileInfos = new List<ArchiveFileInfo>();

            var fileEntryIndex = 0;
            var directoryNameOffset = directoryNamePosition;
            var directoryEntryOffset = directoryEntryPosition;
            foreach (var directoryEntry in _directoryEntries)
            {
                var relevantFiles = directoryEntry.Item2 == null
                    ? Array.Empty<ArchiveFileInfo>()
                    : files.Where(x => x.FilePath.IsInDirectory(((UPath)directoryEntry.Item2).ToAbsolute(), false))
                        .ToArray();
                fileInfos.AddRange(relevantFiles);

                // Write directory name
                bw.BaseStream.Position = directoryNameOffset;
                bw.WriteString(directoryEntry.Item2, Encoding.ASCII, false);

                // Update entry information
                directoryEntry.Item1.fileCount = relevantFiles.Length;
                directoryEntry.Item1.fileEntryIndex = relevantFiles.Length == 0 ? -1 : fileEntryIndex;
                directoryEntry.Item1.directoryNameOffset = directoryNameOffset;

                directoryNameOffset = (int)bw.BaseStream.Position;
                fileEntryIndex += relevantFiles.Length;

                // Write directory entry
                bw.BaseStream.Position = directoryEntryOffset;
                bw.WriteType(directoryEntry);

                directoryEntryOffset = (int)bw.BaseStream.Position;
            }

            var directoryInfoPosition = (directoryNameOffset + 3) & ~3;

            // Write directory infos
            bw.BaseStream.Position = directoryInfoPosition;
            bw.WriteMultiple(_directoryInfos);

            var fileEntryPosition = (int)bw.BaseStream.Position;
            var fileNamePosition = fileEntryPosition + fileInfos.Count * _gar5FileEntrySize;

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

                fileInfos[i].SaveFileData(bw.BaseStream, null);
                bw.WriteAlignment(4);
            }

            // Write file entries
            bw.BaseStream.Position = fileEntryPosition;
            bw.WriteMultiple(fileEntries);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new GarHeader
            {
                directoryCount = (short)_directoryEntries.Count,
                fileCount = (short)fileInfos.Count,

                directoryEntryOffset = directoryEntryPosition,
                fileEntryOffset = fileEntryPosition,
                filePositionOffset = dataPosition,

                fileSize = (uint)bw.BaseStream.Length,

                version = _headerVersion,
                hold0 = _headerString
            });
        }
    }
}
