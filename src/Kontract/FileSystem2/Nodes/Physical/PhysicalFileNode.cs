using System;
using System.IO;
using Kontract.Exceptions.FileSystem;
using Kontract.FileSystem2.Interfaces;
using Kontract.FileSystem2.IO;

namespace Kontract.FileSystem2.Nodes.Physical
{
    public sealed class PhysicalFileNode : IFileNode<IPhysical>, IPhysical
    {
        private readonly string _root;
        private Stream _openedFile;

        public bool IsDirectory => false;
        public string Name { get; }
        public IPhysical Parent { get; set; }
        public string RelativePath => BuildRelativePath();
        public string Root => BuildRoot();

        public bool IsOpened { get; private set; }
        public bool Disposed { get; private set; }

        internal PhysicalFileNode(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        internal PhysicalFileNode(string name, IPhysical parent) : this(name)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public PhysicalFileNode(string name, string root) : this(name)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentException(nameof(root));
            _root = root;
        }

        public Stream Open()
        {
            if (Disposed) throw new ObjectDisposedException(nameof(PhysicalFileNode));

            var filePath = Root + Path.DirectorySeparatorChar + Name;
            if (IsOpened) throw new FileAlreadyOpenException(filePath);

            _openedFile = File.Open(filePath, FileMode.Open);
            var report = new ReportCloseStream(_openedFile);
            report.Closed += Report_Closed;
            IsOpened = true;

            return report;
        }

        private void Report_Closed(object sender, EventArgs e)
        {
            _openedFile = null;
            IsOpened = false;
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

        #region IDisposable

        void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                _openedFile?.Close();
                _openedFile = null;
                Parent = null;
            }

            Disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
        //{
        //    public string RootDir { get; }
        //    public string RootPath => $"{RootDir}{System.IO.Path.DirectorySeparatorChar}{Name}";

        //    public bool IsOpened { get; private set; }

        //    public PhysicalFileNode(string name) : base(name)
        //    {
        //    }

        //    public PhysicalFileNode(string name, string root) : base(name)
        //    {
        //        RootDir = root;
        //    }

        //    public override Stream Open()
        //    {
        //        if (IsOpened) throw new FileAlreadyOpenException(RootPath);

        //        var dir = System.IO.Path.GetDirectoryName(RootPath);
        //        if (!Directory.Exists(dir))
        //            Directory.CreateDirectory(dir);

        //        var report = !File.Exists(RootPath) ?
        //            new ReportCloseStream(File.Create(RootPath)) :
        //            new ReportCloseStream(File.Open(RootPath, FileMode.Open));
        //        report.Closed += Report_Closed;
        //        IsOpened = true;
        //        return report;
        //    }

        //    private void Report_Closed(object sender, System.EventArgs e)
        //    {
        //        IsOpened = false;
        //    }
        //}
    }
}
