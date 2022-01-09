using Kontract.Extensions;
using Kontract.Models.Archive;

namespace Kuriimu2.ImGui.Models
{
    class ArchiveFile
    {
        public IArchiveFileInfo ArchiveFileInfo { get; }

        public string Name => ArchiveFileInfo.FilePath.GetName();

        public long Size => ArchiveFileInfo.FileSize;

        public ArchiveFile(IArchiveFileInfo afi)
        {
            ArchiveFileInfo = afi;
        }
    }
}
