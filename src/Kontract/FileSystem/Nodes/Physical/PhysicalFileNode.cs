using System;
using System.IO;
using Kontract.FileSystem.Exceptions;
using Kontract.FileSystem.IO;
using Kontract.FileSystem.Nodes.Abstract;

namespace Kontract.FileSystem.Nodes.Physical
{
    public sealed class PhysicalFileNode : BaseFileNode
    {
        private readonly string _root;

        private Stream _openedFile;

        public bool IsOpened { get; private set; }

        internal PhysicalFileNode(string name) : base(name)
        {
        }

        internal PhysicalFileNode(string name, PhysicalDirectoryNode parent) : base(name)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public PhysicalFileNode(string name, string root) : base(name)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentException(root);
            _root = root;
        }

        public override Stream Open()
        {
            CheckDisposed();
            if (IsOpened) throw new FileAlreadyOpenException();

            var fullFilePath = BuildRootPath(Name);
            if (!File.Exists(fullFilePath))
            {
                if (!Directory.Exists(Path.GetDirectoryName(fullFilePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(fullFilePath));
                _openedFile = File.Create(fullFilePath);
            }
            else
            {
                _openedFile = File.Open(BuildRootPath(Name), FileMode.Open);
            }

            var report = new ReportCloseStream(_openedFile);
            report.Closed += Report_Closed;
            IsOpened = true;
            return report;
        }

        private void Report_Closed(object sender, EventArgs e)
        {
            IsOpened = false;
            _openedFile = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _openedFile?.Close();
                _openedFile = null;
                IsOpened = false;
            }

            base.Dispose(disposing);
        }

        private string BuildRootPath(string lastElement)
        {
            CheckDisposed();

            return !string.IsNullOrEmpty((Parent as PhysicalDirectoryNode)?.RootPath) ?
                $"{((PhysicalDirectoryNode)Parent).RootPath}{Path.DirectorySeparatorChar}{lastElement}" :
                _root + Path.DirectorySeparatorChar + lastElement;
        }
    }
}
