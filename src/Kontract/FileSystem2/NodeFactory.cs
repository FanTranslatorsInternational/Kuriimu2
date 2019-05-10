using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract.FileSystem2.Nodes.Abstract;
using Kontract.FileSystem2.Nodes.Physical;
using Kontract.FileSystem2.Nodes.Virtual;
using Kontract.Interfaces.Archive;

namespace Kontract.FileSystem2
{
    public static class NodeFactory
    {
        /// <summary>
        /// Create a file node from a given file.
        /// </summary>
        /// <param name="fileName">File to create a node from.</param>
        /// <returns>Created file node.</returns>
        public static BaseFileNode FromFile(string fileName)
        {
            if (!File.Exists(fileName)) throw new FileNotFoundException(fileName);

            return new PhysicalFileNode(Path.GetFileName(fileName));
        }

        /// <summary>
        /// Creates a directory node tree from a given directory.
        /// </summary>
        /// <param name="directory">Directory to create a node tree from.</param>
        /// <returns>Created directory node tree.</returns>
        public static BaseDirectoryNode FromDirectory(string directory)
        {
            if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);

            var unified = Common.UnifyPath(directory).TrimStart('/', '\\');
            return CreatePhysicalNodeTree(unified);
        }

        private static PhysicalDirectoryNode CreatePhysicalNodeTree(string directory)
        {
            var split = directory.Split(Path.DirectorySeparatorChar);

            var result = new PhysicalDirectoryNode($"{Path.DirectorySeparatorChar}{split[0]}");
            foreach (var dir in Directory.EnumerateDirectories(directory))
            {
                result.Add(CreatePhysicalNodeTree(dir));
            }

            foreach (var file in Directory.EnumerateFiles(directory))
            {
                result.Add(new PhysicalFileNode(file));
            }

            return result;
        }

        /// <summary>
        /// Creates a directory node tree from a given collection of <see cref="ArchiveFileInfo"/>s.
        /// </summary>
        /// <param name="afis">Collection of <see cref="ArchiveFileInfo"/>s.</param>
        /// <returns>Created directory node tree.</returns>
        public static BaseDirectoryNode FromArchiveFileInfos(IList<ArchiveFileInfo> afis)
        {
            var result = new VirtualDirectoryNode("");
            foreach (var afi in afis)
            {
                var dir = Path.GetDirectoryName(afi.FileName);
                if (result.ContainsDirectory(dir))
                    result.Add(new VirtualFileNode(Path.GetFileName(afi.FileName), afi));
                else
                {
                    var split = Path.GetDirectoryName(afi.FileName).Split('/', '\\');
                    BaseDirectoryNode dirNode = result;
                    foreach (var dirName in split.Take(split.Length - 1))
                    {
                        var localDir = new VirtualDirectoryNode(dirName);
                        dirNode.Add(localDir);
                        dirNode = localDir;
                    }

                    var file = split.Last();
                    dirNode.Add(new VirtualFileNode(file, afi));
                }
            }

            return result;
        }
    }
}
