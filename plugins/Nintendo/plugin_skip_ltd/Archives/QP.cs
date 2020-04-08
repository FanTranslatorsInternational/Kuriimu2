using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using System.Linq;
using System.Text;
using Kontract.Extensions;

namespace plugin_skip_ltd.Archives
{
    public class QP
    {
        private static int _headerSize = 0x20;
        private static int _entrySize = Tools.MeasureType(typeof(QpEntry));

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            var header = br.ReadType<QpHeader>();

            // Read entries
            br.BaseStream.Position = header.entryDataOffset;
            var rootEntry = br.ReadType<QpEntry>();

            br.BaseStream.Position = header.entryDataOffset;
            var entries = br.ReadMultiple<QpEntry>(rootEntry.fileSize);

            // Read names
            var nameStream = new SubStream(input, br.BaseStream.Position, header.entryDataSize + header.entryDataOffset - br.BaseStream.Position);

            // Add files
            using var nameBr = new BinaryReaderX(nameStream);

            var result = new List<ArchiveFileInfo>();
            var lastDirectoryEntry = entries[0];
            foreach (var entry in entries.Skip(1))
            {
                // A file does not know of its parent directory
                // The tree is structured so that the last directory entry read must hold the current file

                // Remember the last directory entry
                if (entry.IsDirectory)
                {
                    lastDirectoryEntry = entry;
                    continue;
                }

                // Find whole path recursively from lastDirectoryEntry
                var currentDirectoryEntry = lastDirectoryEntry;
                var currentPath = UPath.Empty;
                while (currentDirectoryEntry != entries[0])
                {
                    nameBr.BaseStream.Position = currentDirectoryEntry.NameOffset;
                    currentPath = nameBr.ReadCStringASCII() / currentPath;

                    currentDirectoryEntry = entries[currentDirectoryEntry.fileOffset];
                }

                // Get file name
                nameBr.BaseStream.Position = entry.NameOffset;
                var fileName = currentPath / nameBr.ReadCStringASCII();

                var fileStream = new SubStream(input, entry.fileOffset, entry.fileSize);
                result.Add(new ArchiveFileInfo(fileStream, fileName.FullName));
            }

            return result;
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Build directory tree
            var directoryTree = BuildDirectoryTree(files);

            // Build entries
            var nameStream = new MemoryStream();
            using var nameBw = new BinaryWriterX(nameStream, true);

            var entries = new List<QpEntry>();
            var fileInfos = new List<ArchiveFileInfo>();
            var fileOffset = 0;

            void PopulateEntryList(IList<(UPath, int)> directories, int parentIndex)
            {
                for (var i = 0; i < directories.Count; i++)
                {
                    var currentDirectory = directories[i];

                    // Write directory name
                    var directoryNameOffset = (int)nameBw.BaseStream.Position;
                    var splittedDirectoryName = currentDirectory.Item1.Split();
                    nameBw.WriteString(splittedDirectoryName.Any() ? splittedDirectoryName.Last() : string.Empty, Encoding.ASCII, false);

                    // Add directory
                    var currentDirectoryIndex = entries.Count;
                    var currentDirectoryEntry = new QpEntry
                    {
                        IsDirectory = true,
                        NameOffset = directoryNameOffset,
                        fileOffset = parentIndex
                    };
                    entries.Add(currentDirectoryEntry);

                    // Add files
                    foreach (var file in files.Where(x => x.FilePath.GetDirectory() == currentDirectory.Item1))
                    {
                        fileInfos.Add(file);

                        // Write file name
                        var nameOffset = (int)nameBw.BaseStream.Position;
                        nameBw.WriteString(file.FilePath.GetName(), Encoding.ASCII, false);

                        // Add file entry
                        var fileEntry = new QpEntry
                        {
                            IsDirectory = false,
                            NameOffset = nameOffset,
                            fileOffset = fileOffset,
                            fileSize = (int)file.FileSize
                        };
                        entries.Add(fileEntry);

                        fileOffset = (int)((fileOffset + file.FileSize + 0x1F) & ~0x1F);
                    }

                    // Add sub directories
                    var subDirectories = directories.Where(x => x.Item1.IsInDirectory(currentDirectory.Item1, true)).ToArray();
                    PopulateEntryList(subDirectories, currentDirectoryIndex);

                    // Edit fileSize field
                    currentDirectoryEntry.fileSize = entries.Count;

                    i += subDirectories.Length - 1;
                }
            }

            PopulateEntryList(directoryTree, 0);

            // Adjust file offsets to absolute values
            var dataOffset = (_headerSize + entries.Count * _entrySize + (int)nameStream.Length + 0x1F) & ~0x1F;
            foreach (var entry in entries)
                if (!entry.IsDirectory)
                    entry.fileOffset += dataOffset;

            // Write entries
            bw.BaseStream.Position = _headerSize;
            bw.WriteMultiple(entries);

            // Write names
            nameStream.Position = 0;
            nameStream.CopyTo(bw.BaseStream);
            bw.WriteAlignment(0x20);

            // Write files
            foreach (var fileInfo in fileInfos)
            {
                fileInfo.SaveFileData(bw.BaseStream, null);
                bw.WriteAlignment(0x20);
            }

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new QpHeader
            {
                // TODO: Set hash for header
                hash = 0xFFFFFFFF,
                entryDataOffset = _headerSize,
                entryDataSize = entries.Count * _entrySize + (int)nameStream.Length,
                dataOffset = dataOffset
            });
        }

        private IList<(UPath, int)> BuildDirectoryTree(IReadOnlyList<ArchiveFileInfo> files)
        {
            var distinctDirectories = files
                .OrderBy(x => x.FilePath.GetDirectory())
                .Select(x => x.FilePath.GetDirectory())
                .Distinct();

            var directories = new List<(UPath, int)> { (UPath.Empty, -1) };
            foreach (var directory in distinctDirectories)
            {
                var splittedDirectory = directory.Split();
                for (var i = 0; i < splittedDirectory.Count; i++)
                {
                    var takenParts = splittedDirectory.Take(i + 1).Select(x => (UPath)x).ToArray();
                    var combinedPath = UPath.Combine(takenParts);

                    if (directories.All(x => x.Item1 != combinedPath))
                        directories.Add((combinedPath, directories.FindIndex(x => x.Item1 == combinedPath.GetDirectory())));
                }
            }

            return directories;
        }
    }
}
