using System.Collections.Generic;
using System.Linq;
using Kontract;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.FileSystem;

namespace Komponent.Models
{
    public class DirectoryEntry
    {
        private DirectoryEntry _parent;

        public string Name { get; set; }

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

        /// <summary>
        /// Remove this entry from its parent
        /// </summary>
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
