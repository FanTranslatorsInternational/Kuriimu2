using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Kontract.FileSystem2.Interfaces;

[assembly: InternalsVisibleTo("KontractUnitTests")]

namespace Kontract.FileSystem2.Nodes.Physical
{
    public sealed class PhysicalDirectoryNode : IDirectoryNode<IPhysical>, IPhysical
    {
        //private readonly string _root;

        //public override bool IsDirectory => true;

        //public override IList<BaseNode<PhysicalBaseNode>> Children => GetChildren().ToList();

        //internal PhysicalDirectoryNode(string name) : base(name)
        //{
        //}

        //public PhysicalDirectoryNode(string name, string root) : base(name)
        //{
        //    if (string.IsNullOrEmpty(root)) throw new ArgumentException(nameof(root));
        //    _root = root;
        //}

        //public Stream CreateFile(string fileName)
        //{
        //    if (string.IsNullOrEmpty(fileName)) throw new ArgumentException(fileName);
        //    if (ContainsFile(fileName))
        //        throw new InvalidOperationException("File already exists.");

        //    var split = fileName.Trim('/', '\\').Split('/', '\\');
        //    PhysicalDirectoryNode dir = null;
        //    foreach (var part in split.Take(split.Length - 1))
        //    {
        //        var local = new PhysicalDirectoryNode(part);
        //        dir?.Add(local);
        //        dir = local;
        //    }

        //    var fileNode = new PhysicalFileNode(split.Last());
        //    if (dir == null)
        //        Add(fileNode);
        //    else
        //        dir.Add(fileNode);

        //    return fileNode.Open();
        //}

        //private string GetRootPath()
        //{
        //    if (string.IsNullOrEmpty(_root) && Parent == null)
        //        throw new InvalidOperationException("Root and parent node don't exist.");

        //    return string.IsNullOrEmpty(_root) ? Parent ?:;
        //    return !string.IsNullOrEmpty(RootDir) ?
        //        $"{RootDir}{System.IO.Path.DirectorySeparatorChar}{Name}" :
        //        Name;
        //}

        //private IEnumerable<BaseNode<PhysicalBaseNode>> GetChildren()
        //{
        //    foreach (var dir in System.IO.Directory.EnumerateDirectories(RootDir))
        //    {
        //        var dirName =
        //        yield return new PhysicalDirectoryNode("", dir);
        //    }
        //    foreach (var file in System.IO.Directory.EnumerateFiles(RootDir))
        //        yield return new PhysicalFileNode();
        //}

        private readonly string _root;

        public bool IsDirectory => true;
        public string Name { get; }

        public string RelativePath => BuildRelativePath();
        public string Root => BuildRoot();

        public IPhysical Parent { get; set; }
        public IEnumerable<IPhysical> Children => GetChildren();

        public bool Disposed { get; private set; }

        internal PhysicalDirectoryNode(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        internal PhysicalDirectoryNode(string name, IPhysical parent) : this(name)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public PhysicalDirectoryNode(string name, string root) : this(name)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentException(nameof(root));
            _root = root;
        }

        public bool ContainsDirectory(string directory)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(PhysicalDirectoryNode));

            try
            {
                GetDirectory<PhysicalDirectoryNode>(directory);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ContainsFile(string file)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(PhysicalDirectoryNode));

            try
            {
                GetFile<PhysicalFileNode>(file);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<TDirectory> EnumerateDirectories<TDirectory>() where TDirectory : IDirectoryNode<IPhysical>
        {
            if (Disposed) throw new ObjectDisposedException(nameof(PhysicalDirectoryNode));

            return Children.Where(x => x is TDirectory).Cast<TDirectory>();
        }

        public IEnumerable<TFile> EnumerateFiles<TFile>() where TFile : IFileNode<IPhysical>
        {
            if (Disposed) throw new ObjectDisposedException(nameof(PhysicalDirectoryNode));

            return Children.Where(x => x is TFile).Cast<TFile>();
        }

        public TDirectory GetDirectory<TDirectory>(string relativePath) where TDirectory:IDirectoryNode<IPhysical>
        {
            if (Disposed) throw new ObjectDisposedException(nameof(PhysicalDirectoryNode));
            throw new NotImplementedException();
        }

        public TFile GetFile<TFile>(string relativePath) where TFile:IFileNode<IPhysical>
        {
            if (Disposed) throw new ObjectDisposedException(nameof(PhysicalDirectoryNode));
            throw new NotImplementedException();
        }

        public void Add(IPhysical entry)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(PhysicalDirectoryNode));
            throw new NotImplementedException();
        }

        public void AddRange(IEnumerable<IPhysical> entries)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(PhysicalDirectoryNode));
            throw new NotImplementedException();
        }

        public bool Remove(IPhysical entry)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(PhysicalDirectoryNode));
            throw new NotImplementedException();
        }

        private string BuildRoot()
        {
            if (_root == null && Parent == null)
                throw new InvalidOperationException("No root and parent set.");
            if (_root == null)
                return Parent.Root + Path.DirectorySeparatorChar + Name;
            return _root + Path.DirectorySeparatorChar + Name;
        }

        private string BuildRelativePath()
        {
            if (Parent == null)
                return Name;
            return Parent.Name + Path.DirectorySeparatorChar + Name;
        }

        private IEnumerable<IPhysical> GetChildren()
        {
            foreach (var dir in Directory.EnumerateDirectories(Root))
                yield return new PhysicalDirectoryNode(dir, this);
            foreach (var file in Directory.EnumerateFiles(Root))
                yield return new PhysicalFileNode(file, this);
        }

        #region IDisposable

        void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                Parent = null;
            }

            Disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
