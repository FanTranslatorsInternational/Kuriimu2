using System.Text;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    struct DarcHeader
    {
        public string magic; // darc
        public ushort byteOrder;
        public short headerSize; // 0x1C
        public int version; // 0x1000000
        public int fileSize;
        public int tableOffset; // 0x1C
        public int tableLength;
        public int dataOffset;
    }

    class DarcEntry
    {
        public int tmp1;
        public int offset;
        public int size;        // end index of directory

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

    class DarcArchiveFile : ArchiveFile
    {
        public string UnescapedPath { get; }

        public DarcArchiveFile(ArchiveFileInfo fileInfo, string unescapedPath) : base(fileInfo)
        {
            UnescapedPath = unescapedPath;
        }
    }

    class DarcTreeBuilder
    {
        private Encoding _nameEncoding;
        private BinaryWriterX _nameBw;

        public IList<(DarcEntry, IArchiveFile)> Entries { get; private set; }

        public Stream NameStream { get; private set; }

        public DarcTreeBuilder(Encoding nameEncoding)
        {
            _nameEncoding = nameEncoding;
        }

        public void Build(IList<DarcArchiveFile> files)
        {
            // Build directory tree
            var directoryTree = BuildDirectoryTree(files);

            // Create name stream
            NameStream = new MemoryStream();
            _nameBw = new BinaryWriterX(NameStream, true);

            // Populate entries
            Entries = new List<(DarcEntry, IArchiveFile)>();
            PopulateEntryList(files, directoryTree, 0);
        }

        private IList<(string, int)> BuildDirectoryTree(IList<DarcArchiveFile> files)
        {
            var distinctDirectories = files
                .OrderBy(x => GetDirectory(x.UnescapedPath))
                .Select(x => GetDirectory(x.UnescapedPath))
                .Distinct();

            var directories = new List<(string, int)> { (string.Empty, -1) };
            foreach (var directory in distinctDirectories)
            {
                var splittedDirectory = SplitPath(directory);
                for (var i = 0; i < splittedDirectory.Length; i++)
                {
                    var parentDirectory = Combine(splittedDirectory.Take(i));
                    var currentDirectory = Combine(splittedDirectory.Take(i + 1));

                    if (directories.Any(x => x.Item1 == currentDirectory))
                        continue;

                    var index = directories.FindIndex(x => x.Item1 == parentDirectory);
                    directories.Add((currentDirectory, index));
                }
            }

            return directories;
        }

        private void PopulateEntryList(IList<DarcArchiveFile> files,
            IList<(string, int)> directories, int parentIndex)
        {
            var directoryIndex = 0;
            while (directoryIndex < directories.Count)
            {
                var currentDirectory = directories[directoryIndex];

                // Write directory name
                var directoryNameOffset = (int)_nameBw.BaseStream.Position;
                var splittedDirectoryName = SplitPath(currentDirectory.Item1);
                _nameBw.WriteString(splittedDirectoryName.Any() ? GetName(currentDirectory.Item1) : string.Empty, _nameEncoding);

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
                var filesInDirectory = files.Where(x => GetDirectory(x.UnescapedPath) == currentDirectory.Item1);
                foreach (var file in filesInDirectory)
                {
                    // Write file name
                    var nameOffset = (int)_nameBw.BaseStream.Position;
                    _nameBw.WriteString(GetName(file.UnescapedPath), _nameEncoding);

                    // Add file entry
                    var fileEntry = new DarcEntry
                    {
                        IsDirectory = false,
                        NameOffset = nameOffset
                    };
                    Entries.Add((fileEntry, file));
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
            if (path.EndsWith(Path.DirectorySeparatorChar))
                path = path.Substring(0, path.Length - 1);

            var splitted = path.Split(Path.DirectorySeparatorChar);
            return string.Join(Path.DirectorySeparatorChar, splitted.Take(splitted.Length - 1));
        }

        private string GetName(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar))
                return string.Empty;

            return path.Split(Path.DirectorySeparatorChar).Last();
        }

        private string[] SplitPath(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar))
                path = path.Substring(0, path.Length - 1);

            return path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        }

        private string Combine(IEnumerable<string> parts)
        {
            return string.Join(Path.DirectorySeparatorChar, parts);
        }
    }
}
