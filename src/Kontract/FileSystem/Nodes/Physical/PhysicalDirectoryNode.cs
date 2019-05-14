using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Kontract.FileSystem.Exceptions;
using Kontract.FileSystem.Nodes.Abstract;

[assembly: InternalsVisibleTo("KontractUnitTests")]

namespace Kontract.FileSystem.Nodes.Physical
{
    public sealed class PhysicalDirectoryNode : BaseDirectoryNode<string, string>
    {
        private readonly string _root;

        protected override IEnumerable<BaseNode> ProtectedChildren =>
            BuildChildren();

        public string RootPath => BuildRootPath(Name);

        internal PhysicalDirectoryNode(string name) : base(name)
        {
        }

        private PhysicalDirectoryNode(string name, PhysicalDirectoryNode parent) : base(name)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public PhysicalDirectoryNode(string name, string root) : base(name)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentException(root);
            _root = root;
        }

        public override void AddDirectory(string directory)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException(directory);

            var resolved = ResolvePath(directory);
            BaseReadOnlyDirectoryNode parent;
            for (int i = 0; i < resolved.Split(Path.DirectorySeparatorChar).Count(x => x == ".."); i++)
            {
                parent = Parent;
                if (parent == null)
                    throw new PathOutOfRangeException(directory);
            }

            var unify = Common.UnifyPath(Path.Combine(RootPath, directory));
            if (!Directory.Exists(unify))
                Directory.CreateDirectory(unify);
        }

        public override void AddFile(string file)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(file)) throw new ArgumentException(file);

            var resolved = ResolvePath(file);
            BaseReadOnlyDirectoryNode parent = this;
            for (int i = 0; i < resolved.Split(Path.DirectorySeparatorChar).Count(x => x == ".."); i++)
            {
                parent = parent.Parent;
                if (parent == null)
                    throw new PathOutOfRangeException(file);
            }

            var unify = Common.UnifyPath(Path.Combine(RootPath, file));
            if (!Directory.Exists(Path.GetDirectoryName(unify)))
                Directory.CreateDirectory(Path.GetDirectoryName(unify));
            if (!File.Exists(unify))
                File.Create(unify).Close();
        }

        public override bool RemoveDirectory(string directory)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException(directory);

            var resolved = ResolvePath(directory);
            BaseReadOnlyDirectoryNode parent;
            for (int i = 0; i < resolved.Split(Path.DirectorySeparatorChar).Count(x => x == ".."); i++)
            {
                parent = Parent;
                if (parent == null)
                    throw new PathOutOfRangeException(directory);
            }

            var unify = Common.UnifyPath(Path.Combine(RootPath, directory));
            if (Directory.Exists(unify))
            {
                try
                {
                    Directory.Delete(unify);
                }
                catch (IOException)
                {
                    return false;
                }
            }

            return true;
        }

        public override bool RemoveFile(string file)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(file)) throw new ArgumentException(file);

            var resolved = ResolvePath(file);
            BaseReadOnlyDirectoryNode parent;
            for (int i = 0; i < resolved.Split(Path.DirectorySeparatorChar).Count(x => x == ".."); i++)
            {
                parent = Parent;
                if (parent == null)
                    throw new PathOutOfRangeException(file);
            }

            var unify = Common.UnifyPath(Path.Combine(RootPath, file));
            if (File.Exists(unify))
            {
                try
                {
                    File.Delete(unify);
                }
                catch (IOException)
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerable<BaseNode> BuildChildren()
        {
            CheckDisposed();

            foreach (var dir in Directory.EnumerateDirectories(RootPath))
            {
                var split = dir.Split('\\', '/');
                yield return new PhysicalDirectoryNode(split.Last(), this);
            }

            foreach (var file in Directory.EnumerateFiles(RootPath))
            {
                yield return new PhysicalFileNode(Path.GetFileName(file), this);
            }
        }

        private string BuildRootPath(string lastElement)
        {
            CheckDisposed();

            return !string.IsNullOrEmpty((Parent as PhysicalDirectoryNode)?.RootPath) ?
                $"{((PhysicalDirectoryNode)Parent).RootPath}{Path.DirectorySeparatorChar}{lastElement}" :
                Common.UnifyPath(Path.Combine(_root, lastElement));
        }
    }
}
