using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Exceptions.FileSystem;
using Kontract.FileSystem2.IO;
using Kontract.FileSystem2.Nodes.Abstract;

namespace Kontract.FileSystem2.Nodes.Physical
{
    public sealed class PhysicalFileNode : BaseFileNode
    {
        public string RootDir { get; }
        public string RootPath => $"{RootDir}{System.IO.Path.DirectorySeparatorChar}{Name}";

        public bool IsOpened { get; private set; }

        public PhysicalFileNode(string name) : base(name)
        {
        }

        public PhysicalFileNode(string name, string root) : base(name)
        {
            RootDir = root;
        }

        public override Stream Open()
        {
            if (IsOpened) throw new FileAlreadyOpenException(RootPath);

            var dir = System.IO.Path.GetDirectoryName(RootPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var report = !File.Exists(RootPath) ?
                new ReportCloseStream(File.Create(RootPath)) :
                new ReportCloseStream(File.Open(RootPath, FileMode.Open));
            report.Closed += Report_Closed;
            IsOpened = true;
            return report;
        }

        private void Report_Closed(object sender, System.EventArgs e)
        {
            IsOpened = false;
        }
    }
}
