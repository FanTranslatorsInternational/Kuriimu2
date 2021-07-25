using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Models.Archive;
using Kontract.Models.IO;
#pragma warning disable 649

namespace plugin_nintendo.Archives
{
    class DarcHeader
    {
        [FixedLength(4)]
        public string magic = "darc";
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public ByteOrder byteOrder = ByteOrder.LittleEndian;
        public short headerSize = 0x1C;
        public int version = 0x1000000;
        public int fileSize;
        public int tableOffset = 0x1C;
        public int tableLength;
        public int dataOffset;
    }

    class DarcEntry
    {
        public int tmp1;
        public int offset;
        public int size;

        public bool IsDirectory
        {
            get => tmp1 >> 24 == 1;
            set => tmp1 = (tmp1 & 0xFFFFFF) | ((value ? 1 : 0) << 24);
        }

        public int NameOffset
        {
            get => tmp1 & 0xFFFFFF;
            set => tmp1 = (tmp1 & ~0xFFFFFF) | (value & 0xFFFFFF);
        }
    }

    class DarcTreeBuilder
    {
        private Encoding _nameEncoding;
        private BinaryWriterX _nameBw;

        public IList<(DarcEntry, IArchiveFileInfo)> Entries { get; private set; }

        public Stream NameStream { get; private set; }

        public DarcTreeBuilder(Encoding nameEncoding)
        {
            _nameEncoding = nameEncoding;
        }

        public void Build(IList<(string path, IArchiveFileInfo afi)> files)
        {
            // Build directory tree
            var directoryTree = BuildDirectoryTree(files);

            // Create name stream
            NameStream = new MemoryStream();
            _nameBw = new BinaryWriterX(NameStream, true);

            // Populate entries
            Entries = new List<(DarcEntry, IArchiveFileInfo)>();
            PopulateEntryList(files, directoryTree, 0);
        }

        private IList<(string, int)> BuildDirectoryTree(IList<(string, IArchiveFileInfo)> files)
        {
            var distinctDirectories = files
                .OrderBy(x => GetDirectory(x.Item1))
                .Select(x => GetDirectory(x.Item1))
                .Distinct();

            var directories = new List<(string, int)> { ("/", -1) };
            foreach (var directory in distinctDirectories)
            {
                var splittedDirectory = SplitPath(directory);
                for (var i = 0; i < splittedDirectory.Length; i++)
                {
                    var parentDirectory = "/" + Combine(splittedDirectory.Take(i));
                    var currentDirectory = "/" + Combine(splittedDirectory.Take(i + 1));

                    if (directories.Any(x => x.Item1 == currentDirectory))
                        continue;

                    var index = directories.FindIndex(x => x.Item1 == parentDirectory);
                    directories.Add((currentDirectory, index));
                }
            }

            return directories;
        }

        private void PopulateEntryList(IList<(string path, IArchiveFileInfo afi)> files,
            IList<(string, int)> directories, int parentIndex)
        {
            var directoryIndex = 0;
            while (directoryIndex < directories.Count)
            {
                var currentDirectory = directories[directoryIndex];

                // Write directory name
                var directoryNameOffset = (int)_nameBw.BaseStream.Position;
                var splittedDirectoryName = SplitPath(currentDirectory.Item1);
                _nameBw.WriteString(splittedDirectoryName.Any() ? GetName(currentDirectory.Item1) : string.Empty, _nameEncoding, false);

                // Add directory entry
                var currentDirectoryIndex = Entries.Count;
                var currentDirectoryEntry = new DarcEntry
                {
                    IsDirectory = true,
                    NameOffset = directoryNameOffset,
                    offset = parentIndex
                };
                Entries.Add((currentDirectoryEntry, null));

                // Add file entries
                var filesInDirectory = files.Where(x => GetDirectory(x.path) == currentDirectory.Item1);
                foreach (var file in filesInDirectory)
                {
                    // Write file name
                    var nameOffset = (int)_nameBw.BaseStream.Position;
                    _nameBw.WriteString(GetName(file.path), _nameEncoding, false);

                    // Add file entry
                    var fileEntry = new DarcEntry
                    {
                        IsDirectory = false,
                        NameOffset = nameOffset
                    };
                    Entries.Add((fileEntry, file.afi));
                }

                // Add sub directories
                var subDirectories = directories
                    .Where(x => x != currentDirectory &&
                                x.Item1.StartsWith(currentDirectory.Item1))
                    .ToArray();
                PopulateEntryList(files, subDirectories, currentDirectoryIndex);

                // Edit size of directory
                currentDirectoryEntry.size = Entries.Count;

                directoryIndex += subDirectories.Length + 1;
            }
        }

        private string GetDirectory(string path)
        {
            if (path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);

            var splitted = path.Split("/");
            return string.Join("/", splitted.Take(splitted.Length - 1));
        }

        private string GetName(string path)
        {
            if (path.EndsWith("/"))
                return string.Empty;

            return path.Split("/").Last();
        }

        private string[] SplitPath(string path)
        {
            if (path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);

            return path.Split("/", StringSplitOptions.RemoveEmptyEntries);
        }

        private string Combine(IEnumerable<string> parts)
        {
            return string.Join('/', parts);
        }
    }
}
