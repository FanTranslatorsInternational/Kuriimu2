using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_grezzo.Archives
{
    public class ZAR
    {
        private static int _headerSize = Tools.MeasureType(typeof(ZarHeader));
        private static int _fileTypeEntrySize = Tools.MeasureType(typeof(ZarFileTypeEntry));
        private static int _fileEntrySize = Tools.MeasureType(typeof(ZarFileEntry));

        private string _headerString;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<ZarHeader>();
            _headerString = header.headerString;

            // Read file type entries
            br.BaseStream.Position = header.fileTypeEntryOffset;
            var fileTypeEntries = br.ReadMultiple<ZarFileTypeEntry>(header.fileTypeCount);

            // Read file entries
            br.BaseStream.Position = header.fileEntryOffset;
            var fileEntries = br.ReadMultiple<ZarFileEntry>(header.fileCount);

            // Read file offsets
            br.BaseStream.Position = header.fileOffsetsOffset;
            var fileOffsets = br.ReadMultiple<int>(header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var fileTypeEntry in fileTypeEntries)
            {
                if (fileTypeEntry.fileIndexOffset < 0)
                    continue;

                // Read file indices
                br.BaseStream.Position = fileTypeEntry.fileIndexOffset;
                var fileIndeces = br.ReadMultiple<int>(fileTypeEntry.fileCount);

                foreach (var fileIndex in fileIndeces)
                {
                    var fileStream = new SubStream(input, fileOffsets[fileIndex], fileEntries[fileIndex].fileSize);

                    br.BaseStream.Position = fileEntries[fileIndex].fileNameOffset;
                    var fileName = br.ReadCStringASCII();
                    fileName = fileName.Replace("..\\", "dd\\").Replace(".\\", "d\\");

                    result.Add(new ArchiveFileInfo(fileStream, fileName));
                }
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var fileTypes = files.Select(x => x.FilePath.GetExtensionWithDot()).Distinct().ToArray();

            var fileTypeEntriesPosition = _headerSize;
            var fileTypeNamesPosition = fileTypeEntriesPosition + fileTypes.Length * _fileTypeEntrySize;

            using var bw = new BinaryWriterX(output);

            // Write file types
            var fileTypeEntryOffset = fileTypeEntriesPosition;
            var fileTypeNameOffset = fileTypeNamesPosition;

            var fileInfos = new List<IArchiveFileInfo>();
            var fileIndex = 0;
            foreach (var fileType in fileTypes)
            {
                var relevantFiles = files.Where(x => x.FilePath.GetExtensionWithDot() == fileType).ToArray();
                fileInfos.AddRange(relevantFiles);

                // Write file indices
                var fileIndexOffset = bw.BaseStream.Position = fileTypeNameOffset;
                bw.WriteMultiple(Enumerable.Range(fileIndex, relevantFiles.Length));
                fileIndex += relevantFiles.Length;

                // Write file type name
                var newFileTypeNameOffset = (int)bw.BaseStream.Position;
                bw.WriteString(fileType.Substring(1, fileType.Length - 1), Encoding.ASCII, false);
                bw.WriteAlignment(4);

                fileTypeNameOffset = (int)bw.BaseStream.Position;

                // Write file type
                bw.BaseStream.Position = fileTypeEntryOffset;
                bw.WriteType(new ZarFileTypeEntry
                {
                    fileCount = relevantFiles.Length,
                    fileTypeNameOffset = newFileTypeNameOffset,
                    fileIndexOffset = (int)fileIndexOffset
                });

                fileTypeEntryOffset += _fileTypeEntrySize;
            }

            var fileEntryPosition = fileTypeNameOffset;
            var fileEntryNamePosition = fileEntryPosition + fileInfos.Count * _fileEntrySize;

            // Write file entries
            var fileEntryOffset = fileEntryPosition;
            var fileEntryNameOffset = fileEntryNamePosition;

            foreach (var fileInfo in fileInfos)
            {
                var fileName = fileInfo.FilePath.ToRelative().FullName
                    .Replace('/','\\')
                    .Replace("dd\\", "..\\")
                    .Replace("d\\", ".\\");

                // Write file name
                bw.BaseStream.Position = fileEntryNameOffset;
                bw.WriteString(fileName, Encoding.ASCII, false);
                bw.WriteAlignment(4);
                var newFileEntryNameOffset = bw.BaseStream.Position;

                // Write file entry
                bw.BaseStream.Position = fileEntryOffset;
                bw.WriteType(new ZarFileEntry
                {
                    fileSize = (int)fileInfo.FileSize,
                    fileNameOffset = fileEntryNameOffset
                });

                fileEntryNameOffset = (int)newFileEntryNameOffset;
                fileEntryOffset += _fileEntrySize;
            }

            var fileOffsetsPosition = fileEntryNameOffset;
            var dataPosition = fileOffsetsPosition + fileInfos.Count * 4;

            // Write file offsets
            bw.BaseStream.Position = fileOffsetsPosition;

            var fileOffset = dataPosition;
            foreach (var fileInfo in fileInfos)
            {
                bw.Write(fileOffset);
                fileOffset = (int)((fileOffset + fileInfo.FileSize + 3) & ~3);
            }

            // Write file data
            foreach (var fileInfo in fileInfos.Cast<ArchiveFileInfo>())
            {
                fileInfo.SaveFileData(bw.BaseStream, null);
                bw.WriteAlignment(4);
            }

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new ZarHeader
            {
                fileTypeCount = (short)fileTypes.Length,
                fileCount = (short)fileInfos.Count,

                fileTypeEntryOffset = fileTypeEntriesPosition,
                fileEntryOffset = fileEntryPosition,
                fileOffsetsOffset = fileOffsetsPosition,

                fileSize = (int)bw.BaseStream.Length,

                headerString = _headerString
            });
        }
    }
}
