using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace Kuriimu2.ImGui.Models
{
    internal class ArchiveFile(IArchiveFile afi)
    {
        public IArchiveFile File { get; } = afi;

        public string Name { get; } = afi.FilePath.GetName() ?? string.Empty;

        public long Size { get; } = afi.FileSize;
    }
}
