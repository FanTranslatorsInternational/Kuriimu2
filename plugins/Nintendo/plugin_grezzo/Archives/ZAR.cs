using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_grezzo.Archives
{
    public class ZAR
    {
        private const int HeaderSize_ = 0x20;
        private const int FileTypeEntrySize_ = 0x10;
        private const int FileEntrySize_ = 0x8;

        private string _headerString;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = typeReader.Read<ZarHeader>(br);
            _headerString = header.headerString;

            // Read file type entries
            br.BaseStream.Position = header.fileTypeEntryOffset;
            var fileTypeEntries = typeReader.ReadMany<ZarFileTypeEntry>(br, header.fileTypeCount);

            // Read file entries
            br.BaseStream.Position = header.fileEntryOffset;
            var fileEntries = typeReader.ReadMany<ZarFileEntry>(br, header.fileCount);

            // Read file offsets
            br.BaseStream.Position = header.fileOffsetsOffset;
            var fileOffsets = typeReader.ReadMany<int>(br, header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var fileTypeEntry in fileTypeEntries)
            {
                if (fileTypeEntry.fileIndexOffset < 0)
                    continue;

                // Read file indices
                br.BaseStream.Position = fileTypeEntry.fileIndexOffset;
                var fileIndexes = typeReader.ReadMany<int>(br, fileTypeEntry.fileCount);

                foreach (var fileIndex in fileIndexes)
                {
                    var fileStream = new SubStream(input, fileOffsets[fileIndex], fileEntries[fileIndex].fileSize);

                    br.BaseStream.Position = fileEntries[fileIndex].fileNameOffset;
                    var fileName = br.ReadNullTerminatedString();
                    fileName = fileName.Replace("..\\", "dd\\").Replace(".\\", "d\\");

                    result.Add(new ArchiveFile(new ArchiveFileInfo
                    {
                        FilePath = fileName,
                        FileData = fileStream
                    }));
                }
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            var fileTypes = files.Select(x => x.FilePath.GetExtensionWithDot()).Distinct().ToArray();

            var fileTypeEntriesPosition = HeaderSize_;
            var fileTypeNamesPosition = fileTypeEntriesPosition + fileTypes.Length * FileTypeEntrySize_;

            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Write file types
            var fileTypeEntryOffset = fileTypeEntriesPosition;
            var fileTypeNameOffset = fileTypeNamesPosition;

            var fileInfos = new List<IArchiveFile>();
            var fileIndex = 0;
            foreach (var fileType in fileTypes)
            {
                var relevantFiles = files.Where(x => x.FilePath.GetExtensionWithDot() == fileType).ToArray();
                fileInfos.AddRange(relevantFiles);

                // Write file indices
                var fileIndexOffset = bw.BaseStream.Position = fileTypeNameOffset;
                typeWriter.WriteMany(Enumerable.Range(fileIndex, relevantFiles.Length), bw);
                fileIndex += relevantFiles.Length;

                // Write file type name
                var newFileTypeNameOffset = (int)bw.BaseStream.Position;
                bw.WriteString(fileType.Substring(1, fileType.Length - 1), Encoding.ASCII, false);
                bw.WriteAlignment(4);

                fileTypeNameOffset = (int)bw.BaseStream.Position;

                // Write file type
                bw.BaseStream.Position = fileTypeEntryOffset;
                typeWriter.Write(new ZarFileTypeEntry
                {
                    fileCount = relevantFiles.Length,
                    fileTypeNameOffset = newFileTypeNameOffset,
                    fileIndexOffset = (int)fileIndexOffset
                }, bw);

                fileTypeEntryOffset += FileTypeEntrySize_;
            }

            var fileEntryPosition = fileTypeNameOffset;
            var fileEntryNamePosition = fileEntryPosition + fileInfos.Count * FileEntrySize_;

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
                typeWriter.Write(new ZarFileEntry
                {
                    fileSize = (int)fileInfo.FileSize,
                    fileNameOffset = fileEntryNameOffset
                }, bw);

                fileEntryNameOffset = (int)newFileEntryNameOffset;
                fileEntryOffset += FileEntrySize_;
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
            foreach (var fileInfo in fileInfos)
            {
                fileInfo.WriteFileData(bw.BaseStream);
                bw.WriteAlignment(4);
            }

            // Write header
            bw.BaseStream.Position = 0;
            typeWriter.Write(new ZarHeader
            {
                fileTypeCount = (short)fileTypes.Length,
                fileCount = (short)fileInfos.Count,

                fileTypeEntryOffset = fileTypeEntriesPosition,
                fileEntryOffset = fileEntryPosition,
                fileOffsetsOffset = fileOffsetsPosition,

                fileSize = (int)bw.BaseStream.Length,

                headerString = _headerString
            }, bw);
        }
    }
}
