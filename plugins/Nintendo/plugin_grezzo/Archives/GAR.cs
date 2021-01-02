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

        private static int _gar2FileTypeEntrySize = Tools.MeasureType(typeof(Gar2FileTypeEntry));
        private static int _gar2FileEntrySize = Tools.MeasureType(typeof(Gar2FileEntry));

        private static int _gar5FileTypeEntrySize = 0x20;
        private static int _gar5FileTpyeInfoSize = Tools.MeasureType(typeof(Gar5FileTypeInfo));
        private static int _gar5FileEntrySize = Tools.MeasureType(typeof(Gar5FileEntry));

        private byte _headerVersion;
        private string _headerString;

        private IList<(Gar5FileTypeEntry, string)> _fileTypeEntries;
        private IList<Gar5FileTypeInfo> _fileTypeInfos;

        public IList<IArchiveFileInfo> Load(Stream input)
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

        public void Save(Stream output, IList<IArchiveFileInfo> files)
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

        private IList<IArchiveFileInfo> ParseGar2(BinaryReaderX br, GarHeader header)
        {
            // Read file type entries
            var fileTypeEntries = br.ReadMultiple<Gar2FileTypeEntry>(header.fileTypeCount);

            var result = new List<IArchiveFileInfo>();
            foreach (var fileTypeEntry in fileTypeEntries)
            {
                if (fileTypeEntry.fileIndexOffset < 0)
                    continue;

                // Read file entry indices
                br.BaseStream.Position = fileTypeEntry.fileIndexOffset;
                var fileIndices = br.ReadMultiple<int>(fileTypeEntry.fileCount);

                // Read file entries
                br.BaseStream.Position = header.fileEntryOffset;
                var fileEntries = br.ReadMultiple<Gar2FileEntry>(header.fileCount);

                // Read file offsets
                br.BaseStream.Position = header.fileOffsetsOffset;
                var fileOffsets = br.ReadMultiple<int>(header.fileCount);

                // Add files
                foreach (var fileIndex in fileIndices)
                {
                    var fileStream = new SubStream(br.BaseStream, fileOffsets[fileIndex], fileEntries[fileIndex].fileSize);

                    br.BaseStream.Position = fileEntries[fileIndex].fileNameOffset;
                    var fileName = br.ReadCStringASCII();

                    result.Add(new ArchiveFileInfo(fileStream, fileName));
                }
            }

            return result;
        }

        private IList<IArchiveFileInfo> ParseGar5(BinaryReaderX br, GarHeader header)
        {
            // Read file type entries
            _fileTypeEntries = new List<(Gar5FileTypeEntry, string)>();
            var fileTypeEntries = br.ReadMultiple<Gar5FileTypeEntry>(header.fileTypeCount);

            // Read directory infos
            var fileTypeInfoPosition = fileTypeEntries.Where(x => x.fileTypeInfoOffset > 0).Min(x => x.fileTypeInfoOffset);
            var fileTypeInfoLength = header.fileEntryOffset - fileTypeInfoPosition;
            br.BaseStream.Position = fileTypeInfoPosition;
            _fileTypeInfos = br.ReadMultiple<Gar5FileTypeInfo>(fileTypeInfoLength / _gar5FileTpyeInfoSize);

            var result = new List<IArchiveFileInfo>();
            foreach (var fileTypeEntry in fileTypeEntries)
            {
                // Read file type name
                br.BaseStream.Position = fileTypeEntry.fileTypeNameOffset;
                var fileTypeName = br.ReadCStringASCII();
                _fileTypeEntries.Add((fileTypeEntry, "." + fileTypeName));

                // Read file entries
                br.BaseStream.Position = header.fileEntryOffset;
                var fileEntries = br.ReadMultiple<Gar5FileEntry>(header.fileCount);

                // Add files
                if (fileTypeEntry.fileEntryIndex >= 0)
                {
                    var fileEntryIndexEnd = fileTypeEntry.fileEntryIndex + fileTypeEntry.fileCount;
                    for (var i = fileTypeEntry.fileEntryIndex; i < fileEntryIndexEnd; i++)
                    {
                        var fileStream = new SubStream(br.BaseStream, fileEntries[i].fileOffset, fileEntries[i].fileSize);

                        br.BaseStream.Position = fileEntries[i].fileNameOffset;
                        var fileName = br.ReadCStringASCII();

                        result.Add(new ArchiveFileInfo(fileStream, fileName + "." + fileTypeName));
                    }
                }
            }

            return result;
        }

        private void SaveGar2(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            var fileTypes = files.Select(x => x.FilePath.GetExtensionWithDot()).Distinct().ToArray();

            var fileTypeEntryPosition = _headerSize;
            var fileTypeNamePosition = fileTypeEntryPosition + fileTypes.Length * _gar2FileTypeEntrySize;

            // Write directory entries
            var fileInfos = new List<IArchiveFileInfo>();

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
                bw.WriteMultiple(Enumerable.Range(fileIndex, relevantFiles.Length));
                fileIndex += relevantFiles.Length;

                var newDirectoryNameOffset = (int)bw.BaseStream.Position;

                // Write directory entry
                bw.BaseStream.Position = fileTypeEntryOffset;
                bw.WriteType(new Gar2FileTypeEntry
                {
                    fileCount = relevantFiles.Length,
                    fileTypeNameOffset = fileTypeNameOffset,
                    fileIndexOffset = fileIndexOffset
                });

                fileTypeNameOffset = newDirectoryNameOffset;
                fileTypeEntryOffset += _gar2FileTypeEntrySize;
            }

            var fileEntryPosition = fileTypeNameOffset;

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
            foreach (var fileInfo in fileInfos.Cast<ArchiveFileInfo>())
            {
                fileInfo.SaveFileData(output, null);
                bw.WriteAlignment(4);
            }

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new GarHeader
            {
                fileTypeCount = (short)fileTypes.Length,
                fileCount = (short)fileInfos.Count,

                fileSize = (uint)bw.BaseStream.Length,

                fileTypeEntryOffset = fileTypeEntryPosition,
                fileEntryOffset = fileEntryPosition,
                fileOffsetsOffset = fileOffsetPosition,

                version = _headerVersion,
                hold0 = _headerString
            });
        }

        private void SaveGar5(Stream output, IList<IArchiveFileInfo> files)
        {
            var fileTypeEntryPosition = _headerSize;
            var fileTypeNamePosition = fileTypeEntryPosition + _fileTypeEntries.Count * _gar5FileTypeEntrySize;

            using var bw = new BinaryWriterX(output);

            // Write file type entries
            var fileInfos = new List<IArchiveFileInfo>();

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
                bw.WriteType(fileTypeEntry);

                fileTypeEntryOffset = (int)bw.BaseStream.Position;
            }

            var fileTypeInfoPosition = (fileTypeNameOffset + 3) & ~3;

            // Write file type infos
            bw.BaseStream.Position = fileTypeInfoPosition;
            bw.WriteMultiple(_fileTypeInfos);

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

                (fileInfos[i] as ArchiveFileInfo).SaveFileData(bw.BaseStream, null);
                bw.WriteAlignment(4);
            }

            // Write file entries
            bw.BaseStream.Position = fileEntryPosition;
            bw.WriteMultiple(fileEntries);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new GarHeader
            {
                fileTypeCount = (short)_fileTypeEntries.Count,
                fileCount = (short)fileInfos.Count,

                fileTypeEntryOffset = fileTypeEntryPosition,
                fileEntryOffset = fileEntryPosition,
                fileOffsetsOffset = dataPosition,

                fileSize = (uint)bw.BaseStream.Length,

                version = _headerVersion,
                hold0 = _headerString
            });
        }
    }
}
