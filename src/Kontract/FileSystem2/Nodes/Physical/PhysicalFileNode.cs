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
    internal sealed class PhysicalFileNode : BaseFileNode
    {
        public bool IsOpened { get; private set; }

        public PhysicalFileNode(string name) : base(name)
        {
        }

        public override Stream Open()
        {
            if (!File.Exists(Path)) throw new FileNotFoundException(Path);
            if (IsOpened) throw new FileAlreadyOpenException(Path);

            var report = new ReportCloseStream(File.Open(Path, FileMode.Open));
            report.Closed += Undisposable_Closed;
            IsOpened = true;
            return report;
        }

        private void Undisposable_Closed(object sender, System.EventArgs e)
        {
            IsOpened = false;
        }
    }
}
