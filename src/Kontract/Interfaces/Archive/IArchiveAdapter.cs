using System.Collections.Generic;
using Kontract.Interfaces.Common;

namespace Kontract.Interfaces.Archive
{
    public interface IArchiveAdapter : IPlugin
    {
        List<ArchiveFileInfo> Files { get; }
        
        bool FileHasExtendedProperties { get; }
    }
}
