using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Archive
{
    public interface IArchiveAdapter
    {
        List<ArchiveFileInfo> Files { get; }

        bool CanRenameFiles { get; }
        bool CanReplaceFiles { get; }
        bool FileHasExtendedProperties { get; }
    }
}
