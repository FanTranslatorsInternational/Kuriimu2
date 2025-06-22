using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    struct U8Header
    {
        public uint tag; // 0x55aa382d
        public int entryDataOffset;
        public int entryDataSize;
        public int dataOffset;
    }

    class U8Entry
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

    class DefaultU8FileSystem : BaseU8FileSystem
    {
        public DefaultU8FileSystem(UPath root) : base(root)
        {
        }

        protected override long GetFileOffset(int offset)
        {
            return FileOffsetStart + offset;
        }
    }

    abstract class BaseU8FileSystem
    {
        private BinaryReaderX _nameReader;
        private int _index;

        private readonly UPath _root;

        protected long FileOffsetStart { get; private set; }

        public BaseU8FileSystem(UPath root)
        {
            _root = root;
        }

        public IEnumerable<IArchiveFile> Parse(Stream input, long fileSystemOffset, int fileSystemSize, int fileOffsetStart)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            br.BaseStream.Position = fileSystemOffset;

            // Get root entry
            var root = ReadEntry(br);

            // Get name stream
            var entriesSize = root.size * 0xC;
            var nameStream = new SubStream(input, fileSystemOffset + entriesSize, fileSystemSize - entriesSize);
            _nameReader = new BinaryReaderX(nameStream);

            // Parse entries
            FileOffsetStart = fileOffsetStart;
            br.BaseStream.Position = fileSystemOffset;
            var entries = ReadEntries(br, root.size);
            return ParseDirectory(input, entries);
        }

        private U8Entry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new U8Entry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private U8Entry ReadEntry(BinaryReaderX reader)
        {
            return new U8Entry
            {
                tmp1 = reader.ReadInt32(),
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private IEnumerable<IArchiveFile> ParseDirectory(Stream input, IList<U8Entry> entries)
        {
            var rootEntry = entries[0];
            var endIndex = rootEntry.size;
            _index = 1;

            return ParseDirectory(input, entries, _root, endIndex);
        }

        private IEnumerable<IArchiveFile> ParseDirectory(Stream input, IList<U8Entry> entries, UPath path, int endIndex)
        {
            while (_index < endIndex)
            {
                var entry = entries[_index++];

                _nameReader.BaseStream.Position = entry.NameOffset;
                var nodeName = _nameReader.ReadNullTerminatedString();

                if (entry.IsDirectory)
                {
                    foreach (var file in ParseDirectory(input, entries, path / nodeName, entry.size))
                        yield return file;
                    continue;
                }

                var subStream = new SubStream(input, GetFileOffset(entry.offset), entry.size);
                yield return new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = path / nodeName,
                    FileData = subStream
                });
            }
        }

        protected abstract long GetFileOffset(int offset);
    }

    class U8TreeBuilder
    {
        private Encoding _nameEncoding;
        private BinaryWriterX _nameBw;

        public IList<(U8Entry, IArchiveFile)> Entries { get; private set; }

        public Stream NameStream { get; private set; }

        public U8TreeBuilder(Encoding nameEncoding)
        {
            _nameEncoding = nameEncoding;
        }

        public void Build(IList<(string path, IArchiveFile afi)> files)
        {
            // Build directory tree
            var directoryTree = BuildDirectoryTree(files);

            // Create name stream
            NameStream = new MemoryStream();
            _nameBw = new BinaryWriterX(NameStream, true);

            // Populate entries
            Entries = new List<(U8Entry, IArchiveFile)>();
            PopulateEntryList(files, directoryTree, 0);
        }

        private IList<(string, int)> BuildDirectoryTree(IList<(string, IArchiveFile)> files)
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

        private void PopulateEntryList(IList<(string path, IArchiveFile afi)> files,
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
                var currentDirectoryEntry = new U8Entry
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
                    var fileEntry = new U8Entry
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
            var joined = string.Join("/", splitted.Take(splitted.Length - 1));

            return string.IsNullOrEmpty(joined) ? "/" : joined;
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
