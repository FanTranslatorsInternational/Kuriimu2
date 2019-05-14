using System;
using System.Collections.Generic;
using System.IO;
using Kontract.FileSystem.Exceptions;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.Interfaces.Archive;

namespace Kontract.FileSystem.Nodes.Afi
{
    public sealed class AfiDirectoryNode : BaseDirectoryNode<AfiDirectoryNode, ArchiveFileInfo>
    {
        private readonly IList<BaseNode> _children;

        protected override IEnumerable<BaseNode> ProtectedChildren => _children;

        public AfiDirectoryNode(string name) : base(name)
        {
            _children = new List<BaseNode>();
        }

        public override void AddDirectory(AfiDirectoryNode directory)
        {
            CheckDisposed();
            if (directory == null) throw new ArgumentNullException(nameof(directory));

            directory.Parent = this;
            if (!ContainsDirectory(directory.Name))
                _children.Add(directory);
            else
            {
                var dirNode = GetDirectoryNode(directory.Name) as AfiDirectoryNode;
                foreach (var child in directory.Children)
                    if (child is AfiDirectoryNode dir)
                        dirNode?.AddDirectory(dir);
                    else
                        dirNode?.AddFile((child as AfiFileNode)?.ArchiveFileInfo);
            }
        }

        public override void AddFile(ArchiveFileInfo file)
        {
            CheckDisposed();
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (RelativePath != Path.GetDirectoryName(file.FileName))
                throw new PathMismatchException(this, file);

            var fileNode = new AfiFileNode(Path.GetFileName(file.FileName), file)
            {
                Parent = this
            };
            _children.Add(fileNode);
        }

        public override bool RemoveDirectory(AfiDirectoryNode directory)
        {
            CheckDisposed();
            throw new NotImplementedException();
        }

        public override bool RemoveFile(ArchiveFileInfo file)
        {
            CheckDisposed();
            throw new NotImplementedException();
        }
    }
}
