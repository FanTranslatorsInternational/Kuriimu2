using System.Collections.Generic;
using System.Linq;
using Kontract.Extensions;
using Kontract.Interfaces.Plugins.State.Archive;

namespace Komponent.Extensions
{
    public static class ListExtensions
    {
        public static Models.DirectoryEntry ToTree(this IList<IArchiveFileInfo> files)
        {
            var root = new Models.DirectoryEntry(string.Empty);

            foreach (var file in files)
            {
                var parent = root;

                foreach (var part in file.FilePath.GetDirectory().Split())
                {
                    var entry = parent.Directories.FirstOrDefault(x => x.Name == part);
                    if (entry == null)
                    {
                        entry = new Models.DirectoryEntry(part);
                        parent.AddDirectory(entry);
                    }

                    parent = entry;
                }

                parent.Files.Add(file);
            }

            return root;
        }
    }
}
