using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Kontract.Exceptions.FileSystem;
using Kontract.FileSystem2.Nodes.Abstract;
[assembly:InternalsVisibleTo("KontractUnitTests")]

namespace Kontract.FileSystem2.Nodes.Physical
{
    public sealed class PhysicalDirectoryNode : BaseDirectoryNode
    {
        public string RootDir { get; }
        public string RootPath => $"{RootDir}{System.IO.Path.DirectorySeparatorChar}{Name}";

        internal PhysicalDirectoryNode(string name) : base(name)
        {
        }

        public PhysicalDirectoryNode(string name, string root) : base(name)
        {
            RootDir = root;
        }

        public Stream CreateFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException(fileName);
            if (ContainsFile(fileName))
                throw new InvalidOperationException("File already exists.");

            var split = fileName.Trim('/', '\\').Split('/', '\\');
            PhysicalDirectoryNode dir = null;
            foreach (var part in split.Take(split.Length - 1))
            {
                var local = new PhysicalDirectoryNode(part);
                dir?.Add(local);
                dir = local;
            }

            var fileNode = new PhysicalFileNode(split.Last());
            if (dir == null)
                Add(fileNode);
            else
                dir.Add(fileNode);

            return fileNode.Open();
        }
    }
}
