using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.FileSystem.Nodes.Afi;
using Kontract.FileSystem.Nodes.Physical;
using Kontract.Interfaces.Archive;

namespace Kontract.FileSystem
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
            var unified = Common.UnifyPath(directory).Trim('/', '\\');
            var name = unified.Split(Path.DirectorySeparatorChar);
            return new PhysicalDirectoryNode(name.Last(), Path.GetFullPath(string.Join(Path.DirectorySeparatorChar.ToString(), name.Take(name.Length - 1)) + Path.DirectorySeparatorChar));
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
                if (dir == null)
                {
                    result.AddFile(afi);
                    continue;
                }

                if (result.ContainsDirectory(dir))
                {
                    var dirNode = result.GetDirectoryNode(dir) as AfiDirectoryNode;
                    dirNode?.AddFile(afi);
                }
                else
                {
                    var split = dir.Split('/', '\\');
                    AfiDirectoryNode dirNode = result;
                    foreach (var dirName in split)
                    {
                        if (dirNode.ContainsDirectory(dirName))
                        {
                            dirNode = (AfiDirectoryNode)dirNode.GetDirectoryNode(dirName);
                        }
                        else
                        {
                            var localDir = new AfiDirectoryNode(dirName);
                            dirNode.AddDirectory(localDir);
                            dirNode = localDir;
                        }
                    }

                    dirNode.AddFile(afi);
                }
            }

            return result;
        }
    }
}
