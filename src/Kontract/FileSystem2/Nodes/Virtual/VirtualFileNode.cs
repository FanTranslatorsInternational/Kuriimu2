using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Exceptions.FileSystem;
using Kontract.FileSystem2.IO;
using Kontract.FileSystem2.Nodes.Abstract;
using Kontract.Interfaces.Archive;

namespace Kontract.FileSystem2.Nodes.Virtual
{
    internal sealed class VirtualFileNode : BaseFileNode
    {
        public ArchiveFileInfo ArchiveFileInfo { get; }

        public VirtualFileNode(string name, ArchiveFileInfo afi) : base(name)
        {
            ArchiveFileInfo = afi;
        }

        public override Stream Open()
        {
            var undisposable = new UndisposableStream(ArchiveFileInfo.FileData);
            return undisposable;
        }
    }
}
