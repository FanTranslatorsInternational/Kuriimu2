using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract.FileSystem2.Nodes.Abstract;
using Kontract.FileSystem2.Nodes.Afi;
using Kontract.FileSystem2.Nodes.Physical;
using Kontract.Interfaces.Archive;

namespace Kontract.FileSystem2
{
    public static class NodeFactory
    {
        /// <summary>
        /// Create a file node from a given file. Creates the file if not present yet.
        /// </summary>
        /// <param name="fileName">File to create a node from.</param>
        /// <returns>Created file node.</returns>
        public static PhysicalFileNode FromFile(string fileName)
        {
            return new PhysicalFileNode(Path.GetFileName(fileName), Path.GetDirectoryName(fileName));
        }

        /// <summary>
        /// Creates a directory node tree from a given directory.
        /// </summary>
        /// <param name="directory">Directory to create a node tree from.</param>
        /// <returns>Created directory node tree.</returns>
        public static PhysicalDirectoryNode FromDirectory(string directory)
        {
            var unified = Common.UnifyPath(directory).TrimStart('/', '\\');
            return CreatePhysicalNodeTree(Path.GetFullPath(unified));
        }

        private static PhysicalDirectoryNode CreatePhysicalNodeTree(string directory, bool setRoot = true)
        {
            var split = directory.Split(Path.DirectorySeparatorChar);

            var result = setRoot ?
                new PhysicalDirectoryNode(split.Last(), string.Join(Path.DirectorySeparatorChar.ToString(), split.Take(split.Length - 1).ToArray())) :
                new PhysicalDirectoryNode(split.Last());
            if (!Directory.Exists(directory))
                return result;

            foreach (var dir in Directory.EnumerateDirectories(directory))
            {
                result.Add(CreatePhysicalNodeTree(dir, false));
            }

            foreach (var file in Directory.EnumerateFiles(directory))
            {
                result.Add(new PhysicalFileNode(Path.GetFileName(file)));
            }

            return result;
        }

        /// <summary>
        /// Creates a directory node tree from a given <see cref="ArchiveFileInfo"/>.
        /// </summary>
        /// <param name="afi"><see cref="ArchiveFileInfo"/>.</param>
        /// <returns>Created directory node tree.</returns>
        public static BaseNode FromArchiveFileInfo(ArchiveFileInfo afi)
        {
            return FromArchiveFileInfos(new List<ArchiveFileInfo> { afi }).Children.FirstOrDefault();
        }

        /// <summary>
        /// Creates a directory node tree from a given collection of <see cref="ArchiveFileInfo"/>s.
        /// </summary>
        /// <param name="afis">Collection of <see cref="ArchiveFileInfo"/>s.</param>
        /// <returns>Created directory node tree.</returns>
        public static AfiDirectoryNode FromArchiveFileInfos(IList<ArchiveFileInfo> afis)
        {
            var result = new AfiDirectoryNode("");
            foreach (var afi in afis)
            {
                var dir = Path.GetDirectoryName(afi.FileName);
                if (result.ContainsDirectory(dir))
                {
                    var dirNode = result.GetDirectoryNode(dir);
                    dirNode.Add(new AfiFileNode(Path.GetFileName(afi.FileName), afi));
                }
                else
                {
                    var split = dir.Split('/', '\\');
                    BaseDirectoryNode dirNode = result;
                    foreach (var dirName in split)
                    {
                        var localDir = new AfiDirectoryNode(dirName);
                        dirNode.Add(localDir);
                        dirNode = localDir;
                    }

                    dirNode.Add(new AfiFileNode(Path.GetFileName(afi.FileName), afi));
                }
            }

            return result;
        }
    }
}
