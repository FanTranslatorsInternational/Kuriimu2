using Konnect.Contract.Plugin.File.Archive;
using Konnect.DataClasses.FileSystem;

namespace Konnect.Extensions;

public static class ListExtensions
{
    public static DirectoryEntry ToTree(this IList<IArchiveFile> files)
    {
        var root = new DirectoryEntry(string.Empty);

        foreach (IArchiveFile file in files)
        {
            DirectoryEntry parent = root;

            foreach (string part in file.FilePath.GetDirectory().Split())
            {
                DirectoryEntry? entry = parent.Directories.FirstOrDefault(x => x.Name == part);
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