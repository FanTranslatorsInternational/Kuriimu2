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
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);
            _headerString = header.headerString;

            // Read file type entries
            br.BaseStream.Position = header.fileTypeEntryOffset;
            var fileTypeEntries = ReadFileTypeEntries(br, header.fileTypeCount);

            // Read file entries
            br.BaseStream.Position = header.fileEntryOffset;
            var fileEntries = ReadFileEntries(br, header.fileCount);

            // Read file offsets
            br.BaseStream.Position = header.fileOffsetsOffset;
            var fileOffsets = ReadIntegers(br, header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var fileTypeEntry in fileTypeEntries)
            {
                if (fileTypeEntry.fileIndexOffset < 0)
                    continue;

                // Read file indices
                br.BaseStream.Position = fileTypeEntry.fileIndexOffset;
                var fileIndexes = ReadIntegers(br, fileTypeEntry.fileCount);

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
                WriteIntegers(Enumerable.Range(fileIndex, relevantFiles.Length).ToArray(), bw);
                fileIndex += relevantFiles.Length;

                // Write file type name
                var newFileTypeNameOffset = (int)bw.BaseStream.Position;
                bw.WriteString(fileType.Substring(1, fileType.Length - 1), Encoding.ASCII);
                bw.WriteAlignment(4);

                fileTypeNameOffset = (int)bw.BaseStream.Position;

                // Write file type
                var fileTypeEntry = new ZarFileTypeEntry
                {
                    fileCount = relevantFiles.Length,
                    fileTypeNameOffset = newFileTypeNameOffset,
                    fileIndexOffset = (int)fileIndexOffset,
                    unk1 = -1
                };

                bw.BaseStream.Position = fileTypeEntryOffset;
                WriteFileTypeEntry(fileTypeEntry, bw);

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
                    .Replace('/', '\\')
                    .Replace("dd\\", "..\\")
                    .Replace("d\\", ".\\");

                // Write file name
                bw.BaseStream.Position = fileEntryNameOffset;
                bw.WriteString(fileName, Encoding.ASCII, false);
                bw.WriteAlignment(4);
                var newFileEntryNameOffset = bw.BaseStream.Position;

                // Write file entry
                var fileEntry = new ZarFileEntry
                {
                    fileSize = (int)fileInfo.FileSize,
                    fileNameOffset = fileEntryNameOffset
                };

                bw.BaseStream.Position = fileEntryOffset;
                WriteFileEntry(fileEntry, bw);

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
            var header = new ZarHeader
            {
                magic = "ZAR",
                version = 1,

                fileTypeCount = (short)fileTypes.Length,
                fileCount = (short)fileInfos.Count,

                fileTypeEntryOffset = fileTypeEntriesPosition,
                fileEntryOffset = fileEntryPosition,
                fileOffsetsOffset = fileOffsetsPosition,

                fileSize = (int)bw.BaseStream.Length,

                headerString = _headerString
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);
        }

        private ZarHeader ReadHeader(BinaryReaderX reader)
        {
            return new ZarHeader
            {
                magic = reader.ReadString(3),
                version = reader.ReadByte(),
                fileSize = reader.ReadInt32(),
                fileTypeCount = reader.ReadInt16(),
                fileCount = reader.ReadInt16(),
                fileTypeEntryOffset = reader.ReadInt32(),
                fileEntryOffset = reader.ReadInt32(),
                fileOffsetsOffset = reader.ReadInt32(),
                headerString = reader.ReadString(8)
            };
        }

        private ZarFileTypeEntry[] ReadFileTypeEntries(BinaryReaderX reader, int count)
        {
            var result = new ZarFileTypeEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadFileTypeEntry(reader);

            return result;
        }

        private ZarFileTypeEntry ReadFileTypeEntry(BinaryReaderX reader)
        {
            return new ZarFileTypeEntry
            {
                fileCount = reader.ReadInt32(),
                fileIndexOffset = reader.ReadInt32(),
                fileTypeNameOffset = reader.ReadInt32(),
                unk1 = reader.ReadInt32()
            };
        }

        private ZarFileEntry[] ReadFileEntries(BinaryReaderX reader, int count)
        {
            var result = new ZarFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadFileEntry(reader);

            return result;
        }

        private ZarFileEntry ReadFileEntry(BinaryReaderX reader)
        {
            return new ZarFileEntry
            {
                fileSize = reader.ReadInt32(),
                fileNameOffset = reader.ReadInt32()
            };
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteHeader(ZarHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.fileSize);
            writer.Write(header.fileTypeCount);
            writer.Write(header.fileCount);
            writer.Write(header.fileTypeEntryOffset);
            writer.Write(header.fileEntryOffset);
            writer.Write(header.fileOffsetsOffset);
            writer.WriteString(header.headerString, writeNullTerminator: false);
        }

        private void WriteFileTypeEntry(ZarFileTypeEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.fileCount);
            writer.Write(entry.fileIndexOffset);
            writer.Write(entry.fileTypeNameOffset);
            writer.Write(entry.unk1);
        }

        private void WriteFileEntry(ZarFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.fileSize);
            writer.Write(entry.fileNameOffset);
        }

        private void WriteIntegers(int[] entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }
    }
}
