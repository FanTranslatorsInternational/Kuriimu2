using System.Collections.Generic;
using System.Linq;
using Kontract;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace Komponent.Extensions
{
    public static class ListExtensions
    {
        public static DirectoryEntry ToTree(this IList<IArchiveFileInfo> files)
        {
            var root = new DirectoryEntry("");

            foreach (var file in files)
            {
                var parent = root;

                foreach (var part in file.FilePath.GetDirectory().Split())
                {
                    var entry = parent.Directories.FirstOrDefault(x => x.Name == part);
                    if (entry == null)
                    {
                        entry = new DirectoryEntry(part);
                        parent.AddDirectory(entry);
                    }

                    parent = entry;
                }

                parent.Files.Add(file);
            }

            return root;
        }
    }

    public class DirectoryEntry
    {
        private DirectoryEntry _parent;

        public string Name { get; }

        public UPath AbsolutePath => CreateAbsolutePath();

        public IList<DirectoryEntry> Directories { get; }

        public IList<IArchiveFileInfo> Files { get; }

        public DirectoryEntry(string name)
        {
            ContractAssertions.IsNotNull(name, nameof(name));

            Name = name;
            Directories = new List<DirectoryEntry>();
            Files = new List<IArchiveFileInfo>();
        }

        /// <summary>
        /// Adds or merges a directory entry into this one.
        /// </summary>
        /// <param name="entry"></param>
        public void AddDirectory(DirectoryEntry entry)
        {
            var existingDir = Directories.FirstOrDefault(x => x.Name == entry.Name);
            if (existingDir == null)
            {
                entry._parent = this;
                Directories.Add(entry);
                return;
            }

            foreach (var dir in entry.Directories)
                existingDir.AddDirectory(dir);
            foreach (var file in entry.Files)
                if (!existingDir.Files.Contains(file))
                    existingDir.Files.Add(file);
        }

        public void Remove()
        {
            _parent?.Directories.Remove(this);
            _parent = null;
        }

        private UPath CreateAbsolutePath()
        {
            if (_parent == null)
                return Name;

            return _parent.AbsolutePath / Name;
        }
    }
}
