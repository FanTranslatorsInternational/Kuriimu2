using System;
using System.IO;
using Kontract.FileSystem.IO;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.Interfaces.Archive;

namespace Kontract.FileSystem.Nodes.Afi
{
    public sealed class AfiFileNode : BaseFileNode
    {
        public ArchiveFileInfo ArchiveFileInfo { get; }

        public AfiFileNode(string name, ArchiveFileInfo afi) : base(name)
        {
            ArchiveFileInfo = afi ?? throw new ArgumentNullException(nameof(afi));
        }

        public override Stream Open()
        {
            CheckDisposed();

            var undisposable = new UndisposableStream(ArchiveFileInfo.FileData);
            return undisposable;
        }
    }
}
