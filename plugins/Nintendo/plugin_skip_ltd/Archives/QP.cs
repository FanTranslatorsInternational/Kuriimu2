using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using System.Linq;
using Kontract.Extensions;

namespace plugin_skip_ltd.Archives
{
    public class QP
    {
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
            // Build directory tree
            var directoryTree = BuildDirectoryTree(files);


        }

        private IList<UPath> BuildDirectoryTree(IReadOnlyList<ArchiveFileInfo> files)
        {
            var distinctDirectories = files
                .OrderBy(x => x.FilePath.GetDirectory())
                .Select(x => x.FilePath.GetDirectory())
                .Distinct();

            var directories = new List<UPath> { UPath.Root };
            foreach (var directory in distinctDirectories)
            {
                var splittedDirectory = directory.Split();
                for (var i = 0; i < splittedDirectory.Count; i++)
                {
                    var takenParts = splittedDirectory.Take(i + 1).Select(x => (UPath)x).ToArray();
                    var combinedPath = UPath.Combine(takenParts);

                    if (!directories.Contains(combinedPath))
                        directories.Add(combinedPath);
                }
            }

            return directories;
        }
    }
}
