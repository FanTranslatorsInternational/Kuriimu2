using Kontract.Extensions;
using Kontract.Interfaces.Plugins.State.Archive;

namespace Kuriimu2.ImGui.Models
{
    class ArchiveFile
    {
        public IArchiveFileInfo ArchiveFileInfo { get; }

        public string Name { get; }

        public long Size { get; }

        public ArchiveFile(IArchiveFileInfo afi)
        {
            ArchiveFileInfo = afi;

            // Set them explicitly instead of a getter, to avoid potential race conditions from the rendering loop accessing this class
            Name = ArchiveFileInfo.FilePath.GetName();
            Size = ArchiveFileInfo.FileSize;
        }
    }
}
